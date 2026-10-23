import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';

import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { todayIso } from '../../../core/utils/date-helper';
import { renderMarkdown } from '../../../core/utils/markdown';
import { SharedModule } from '../../../shared/shared.module';
import { Category, CategoryCrud } from '../../catalogs/category.crud';
import { Laboratory, LaboratoryCrud } from '../../catalogs/laboratory.crud';
import { UnitOfMeasurement, UnitOfMeasurementCrud } from '../../catalogs/unit-of-measurement.crud';
import { AlternativePickerModalComponent } from '../components/alternative-picker-modal.component';
import { ProductBranchSettingsComponent } from '../components/product-branch-settings.component';
import { ProductImageUploaderComponent } from '../components/product-image-uploader.component';
import {
  PharmaCatalogItem, PharmaSubstance, ProductComponent, ProductEquivalent, ProductPharma,
  UNIDADES_CONCENTRACION, newProductComponent, newProductPharma,
} from './pharma.model';
import { PharmaService } from './pharma.service';
import { Product, newProduct } from './product.model';
import { ProductService } from './product.service';

/** Alta y edición de un producto: ficha, datos por sucursal, rubro farmacia y trazabilidad. */
@Component({
  selector: 'app-product-edit',
  imports: [FormsModule, SharedModule, ProductImageUploaderComponent, ProductBranchSettingsComponent, AlternativePickerModalComponent, FormActionsComponent],
  templateUrl: './product-edit.component.html',
  // El prospecto usa títulos de markdown; sin esto un `##` se vería enorme dentro de un formulario.
  styles: `
    :host ::ng-deep .prospecto-previa :is(h1, h2, h3) { font-size: 1rem; font-weight: 600; margin-top: .75rem; }
    :host ::ng-deep .prospecto-previa p { margin-bottom: .5rem; }
    :host ::ng-deep .prospecto-previa ul { margin-bottom: .5rem; padding-left: 1.25rem; }
  `,
})
export class ProductEditComponent implements OnInit {
  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.save(), disabled: () => this.isReadOnly },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly products = inject(ProductService);
  private readonly pharmaService = inject(PharmaService);
  private readonly laboratoryCrud = inject(LaboratoryCrud);
  private readonly categoryCrud = inject(CategoryCrud);
  private readonly uomCrud = inject(UnitOfMeasurementCrud);

  protected readonly UNIDADES_CONCENTRACION = UNIDADES_CONCENTRACION;

  protected product: Product = newProduct();
  protected laboratories: Laboratory[] = [];
  protected categories: Category[] = [];
  protected unitsOfMeasurement: UnitOfMeasurement[] = [];

  protected submitted = false;
  protected isSaved = false;

  // Imagen elegida antes de que el producto exista: se sube justo después de crearlo.
  private pendingImage: { file: Blob; name: string } | null = null;

  // ── Rubro farmacia ──────────────────────────────────────
  protected pharma: ProductPharma = newProductPharma();
  protected formas: PharmaCatalogItem[] = [];
  protected vias: PharmaCatalogItem[] = [];
  protected sustancias: PharmaSubstance[] = [];
  protected equivalentes: ProductEquivalent[] = [];
  /** La vuelta de la relación: fichas donde ESTE producto se ofrece. */
  protected sugeridoEn: ProductEquivalent[] = [];

  protected prospecto = '';
  protected prospectoAbierto = false;
  protected prospectoCargado = false;
  protected verPrevia = false;

  // ── Selector de alternativas ────────────────────────────
  protected readonly selectorAbierto = signal(false);
  protected preseleccion: Product | null = null;
  protected motivoPreseleccion = '';

  protected get isNew(): boolean {
    return this.product.Id === '0';
  }

  /** Permiso efectivo para grabar: crear si es nuevo, actualizar si es edición. */
  protected get canSave(): boolean {
    return this.permissions.can('products-admin', this.isNew ? 'create' : 'update');
  }

  /** El formulario queda bloqueado si ya se grabó o si el usuario no puede grabar. */
  protected get isReadOnly(): boolean {
    return this.isSaved || !this.canSave;
  }

  protected get trackingText(): string {
    if (this.product.TrackingMode === 'lot') return 'Por lotes';
    if (this.product.TrackingMode === 'serial') return 'Por número de serie';
    return 'Sin seguimiento';
  }

  /** Mismo criterio de contraste que el resto de la app: variantes `subtle` + `emphasis`. */
  protected get trackingBadge(): string {
    const base = 'badge border';
    return this.product.TrackingMode === 'none'
      ? `${base} bg-secondary-subtle text-secondary-emphasis border-secondary-subtle`
      : `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
  }

  /** Nivel de stock contra el mínimo, para colorear el campo. */
  protected get stockClass(): string {
    const p = this.product;
    if (!p.MinReorderQuantity) return '';
    if (p.CurrentStock === 0) return 'text-danger fw-bold';
    if (p.CurrentStock <= p.MinReorderQuantity) return 'text-danger';
    if (p.CurrentStock <= p.MinReorderQuantity * 1.5) return 'text-warning';
    return 'text-success';
  }

  /** Un registro vencido no debería venderse; se avisa al editar. */
  protected get registroVencido(): boolean {
    return !!this.pharma.SanitaryRegistryExpiry && this.pharma.SanitaryRegistryExpiry < todayIso();
  }

  protected get equivalentesAutomaticos(): ProductEquivalent[] {
    return this.equivalentes.filter(e => !e.IsManual);
  }

  /** Solo las manuales: las automáticas se muestran aparte y no se administran. */
  protected get alternativasManuales(): ProductEquivalent[] {
    return this.equivalentes.filter(e => e.IsManual);
  }

  /** Si alguna tiene posición, el orden dejó de ser automático. */
  protected get hayOrdenFijado(): boolean {
    return this.alternativasManuales.some(a => (a.ShowOrder ?? 0) > 0);
  }

  /**
   * Lo que el selector NO debe ofrecer: lo ya sugerido y lo que el sistema deduce
   * solo por composición. Ofrecerlos sería invitar a duplicar algo resuelto.
   */
  protected get idsExcluidos(): string[] {
    return this.equivalentes.map(e => e.ProductId);
  }

  protected get prospectoHtml(): SafeHtml {
    // El HTML ya viene saneado por DOMPurify (renderMarkdown); Angular lo
    // volvería a recortar, así que se marca como confiable.
    return this.sanitizer.bypassSecurityTrustHtml(renderMarkdown(this.prospecto));
  }

  // ── Validaciones (se muestran tras intentar grabar) ─────
  protected get errors() {
    const p = this.product;
    return {
      ProductCode: !p.ProductCode?.trim(),
      BarCode: !p.BarCode?.trim(),
      ProductName: !p.ProductName?.trim(),
      Description: !p.Description?.trim(),
      UomId: !p.UomId,
      SalePrice: !(p.SalePrice > 0),
      MinReorderQuantity: !(p.MinReorderQuantity > 0),
    };
  }

  protected get hasErrors(): boolean {
    return Object.values(this.errors).some(Boolean);
  }

  protected formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  async ngOnInit() {
    this.product.Id = this.route.snapshot.paramMap.get('id') || '0';

    if (!this.isNew) {
      await this.loadProduct(this.product.Id);
      await this.cargarRubroFarmacia(this.product.Id);
    }
    const [labs, cats, uoms] = await Promise.all([
      this.laboratoryCrud.list(''),
      this.categoryCrud.list(''),
      this.uomCrud.list(''),
    ]);
    if (labs.ok) this.laboratories = labs.Data;
    if (cats.ok) this.categories = cats.Data;
    if (uoms.ok) this.unitsOfMeasurement = uoms.Data;
  }

  private async loadProduct(productId: string) {
    const { ok, Data } = await this.products.getById(productId);
    if (!ok) return;

    this.product = {
      ...Data,
      // La ficha edita el catálogo: precio y mínimo base, no los de la sucursal
      // activa. Si no, guardar copiaría la excepción de esta sucursal a todas.
      SalePrice: Data.BaseSalePrice ?? Data.SalePrice,
      MinReorderQuantity: Data.BaseMinReorderQuantity ?? Data.MinReorderQuantity,
      // Laboratorio y categoría son opcionales y llegan con null; el <select>
      // compara contra el value="" de su opción «sin asignar».
      LaboratoryId: Data.LaboratoryId ?? '',
      CategoryId: Data.CategoryId ?? '',
    };
  }

  back() {
    this.router.navigate(['products-admin']);
  }

  protected onImagePicked(event: { file: Blob | null; name: string }) {
    this.pendingImage = event.file ? { file: event.file, name: event.name } : null;
  }

  async save() {
    if (!this.canSave) {
      await this.msg.showMessageModal({ Description: 'No tiene permiso para guardar productos.', MessageType: 'warning' });
      return;
    }

    this.submitted = true;
    if (this.hasErrors) return;
    if (!(await this.msg.showMessageQuestion('¿Desea guardar el producto?'))) return;

    if (this.isNew) {
      const { ok, Data: idProduct } = await this.products.create(this.product);
      if (!ok) return;

      this.isSaved = true;
      this.product.Id = idProduct;

      // El producto ya quedó creado: si la imagen falla, el error se ve pero no
      // se pierde nada; se puede subir después desde la edición.
      if (this.pendingImage) {
        const img = await this.products.uploadImage(idProduct, this.pendingImage.file, this.pendingImage.name);
        if (img.ok) this.product.ImagePath = img.Data;
        this.pendingImage = null;
      }
      await this.msg.showMessageModal({ Description: 'El producto se creó correctamente.', MessageType: 'success' });
      return;
    }

    const { ok, Data: okResp } = await this.products.update(this.product);
    if (!ok) return;

    // Los datos del rubro se guardan aparte —son de otro endpoint— pero detrás
    // de un solo botón: para quien carga, es una sola acción.
    const guardado = await this.pharmaService.savePharma(this.product.Id, this.pharma);

    // Solo si se abrió la sección: si nadie la tocó, `prospecto` está vacío y
    // guardarlo borraría el que ya existía en la base.
    if (this.prospectoCargado) {
      await this.pharmaService.saveLeaflet(this.product.Id, this.prospecto);
    }

    this.isSaved = okResp;
    if (okResp && guardado.ok) {
      await this.msg.showMessageModal({ Description: 'El producto se actualizó correctamente.', MessageType: 'success' });
      // La composición pudo cambiar, y con ella los equivalentes.
      const eq = await this.pharmaService.getEquivalents(this.product.Id);
      if (eq.ok) this.equivalentes = eq.Data;
    }
  }

  // ── Rubro farmacia ──────────────────────────────────────
  /** Los catálogos se traen una vez; el listado de sustancias alimenta el datalist. */
  private async cargarRubroFarmacia(productId: string) {
    const [f, v, s] = await Promise.all([
      this.pharmaService.getForms(),
      this.pharmaService.getRoutes(),
      this.pharmaService.searchSubstances(''),
    ]);
    if (f.ok) this.formas = f.Data;
    if (v.ok) this.vias = v.Data;
    if (s.ok) this.sustancias = s.Data;

    const { ok, Data } = await this.pharmaService.getByProduct(productId);
    if (ok && Data) {
      this.pharma = {
        ...newProductPharma(),
        ...Data,
        FormId: Data.FormId ?? '',
        RouteId: Data.RouteId ?? '',
        Presentation: Data.Presentation ?? '',
        DosageReference: Data.DosageReference ?? '',
        ProductType: Data.ProductType ?? '',
        SanitaryRegistry: Data.SanitaryRegistry ?? '',
        SanitaryRegistryExpiry: (Data.SanitaryRegistryExpiry ?? '').toString().substring(0, 10),
        Components: (Data.Components ?? []).map(c => ({ ...newProductComponent(), ...c, ConcentrationUnit: c.ConcentrationUnit ?? '' })),
      };
    }
    await this.recargarRelaciones(productId);
  }

  /** Las dos direcciones se leen juntas: una sin la otra cuenta media historia. */
  private async recargarRelaciones(productId: string) {
    const [eq, inv] = await Promise.all([
      this.pharmaService.getEquivalents(productId),
      this.pharmaService.getSuggestedIn(productId),
    ]);
    if (eq.ok) this.equivalentes = eq.Data;
    if (inv.ok) this.sugeridoEn = inv.Data;
  }

  protected agregarComponente() {
    this.pharma.Components.push(newProductComponent());
  }

  protected quitarComponente(index: number) {
    this.pharma.Components.splice(index, 1);
  }

  /**
   * Al escribir la sustancia se busca el id en el catálogo. Si no coincide con
   * ninguna, queda solo el nombre y el servidor la da de alta al guardar: cargar
   * un producto no debería obligar a salir a crear el principio activo primero.
   */
  protected onSustancia(componente: ProductComponent, nombre: string) {
    componente.SubstanceName = nombre;
    const conocida = this.sustancias.find(s => s.SubstanceName.toUpperCase() === nombre.trim().toUpperCase());
    componente.SubstanceId = conocida?.Id ?? '';
  }

  /**
   * Se trae la primera vez que se abre, no al cargar la ficha: es lo que hace
   * que el peso del prospecto no lo pague quien solo viene a cambiar un precio.
   */
  protected async alternarProspecto() {
    this.prospectoAbierto = !this.prospectoAbierto;
    if (!this.prospectoAbierto || this.prospectoCargado) return;

    const { ok, Data } = await this.pharmaService.getLeaflet(this.product.Id);
    if (ok) this.prospecto = Data ?? '';
    this.prospectoCargado = true;
  }

  // ── Alternativas sugeridas ──────────────────────────────
  protected abrirSelectorAlternativa(producto: Product | null = null, motivo = '') {
    this.preseleccion = producto;
    this.motivoPreseleccion = motivo;
    this.selectorAbierto.set(true);
  }

  /**
   * Atajo desde los equivalentes automáticos: se abre el selector con ese producto
   * ya elegido. No se agrega de una porque el motivo es lo que el vendedor lee, y
   * conviene confirmarlo antes de fijarlo.
   */
  protected sugerirEquivalente(e: ProductEquivalent) {
    const comoProducto = { Id: e.ProductId, ProductName: e.ProductName, SalePrice: e.SalePrice, CurrentStock: e.CurrentStock } as Product;
    this.abrirSelectorAlternativa(comoProducto, 'Misma composición');
  }

  protected async confirmarAlternativa(payload: { productId: string; reason: string }) {
    const { ok } = await this.pharmaService.addAlternative(this.product.Id, payload.productId, payload.reason);
    this.selectorAbierto.set(false);
    if (ok) await this.recargarRelaciones(this.product.Id);
  }

  /**
   * Mueve una sugerencia en la lista. Al mover, el orden pasa a ser explícito:
   * se guarda la posición de TODAS, porque a medias el resultado en pantalla
   * sería difícil de predecir.
   */
  protected async moverAlternativa(indice: number, salto: number) {
    const lista = [...this.alternativasManuales];
    const destino = indice + salto;
    if (destino < 0 || destino >= lista.length) return;

    [lista[indice], lista[destino]] = [lista[destino], lista[indice]];
    const { ok } = await this.pharmaService.setAlternativesOrder(this.product.Id, lista.map(a => a.ProductId));
    if (ok) await this.recargarRelaciones(this.product.Id);
  }

  /** Devuelve la decisión al sistema: disponibilidad primero y después precio. */
  protected async ordenAutomatico() {
    const { ok } = await this.pharmaService.setAlternativesOrder(this.product.Id, []);
    if (ok) await this.recargarRelaciones(this.product.Id);
  }

  protected async quitarAlternativa(alternativeId: string) {
    const { ok } = await this.pharmaService.removeAlternative(this.product.Id, alternativeId);
    if (ok) await this.recargarRelaciones(this.product.Id);
  }

  // ── Trazabilidad ────────────────────────────────────────
  /**
   * La activación es irreversible, así que se confirma con las consecuencias a la
   * vista y se recarga la ficha desde el servidor: el modo lo decide él.
   */
  protected async activateTrackingMode(modo: 'lot' | 'serial') {
    const porLotes = modo === 'lot';
    const que = porLotes ? 'el lote' : 'un número de serie por unidad';

    const confirmado = await this.msg.showMessageQuestion(
      `¿Activar el control por ${porLotes ? 'lotes' : 'números de serie'} en "${this.product.ProductName}"? ` +
      `Desde ahora cada recepción pedirá ${que}. Esta acción no se puede deshacer.`);
    if (!confirmado) return;

    const { ok } = await this.products.activateTracking(this.product.Id, modo);
    if (!ok) return;

    await this.loadProduct(this.product.Id);
    await this.msg.showMessageModal({
      Description: porLotes ? 'El producto ahora se controla por lotes.' : 'El producto ahora se controla por números de serie.',
      MessageType: 'success',
    });
  }
}
