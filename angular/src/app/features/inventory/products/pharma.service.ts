import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { PharmaCatalogItem, PharmaSubstance, ProductEquivalent, ProductPharma } from './pharma.model';

@Injectable({ providedIn: 'root' })
export class PharmaService {
  private readonly api = inject(ApiService);

  getForms() {
    return this.api.get<ResponseArray<PharmaCatalogItem>>('Pharma/forms');
  }

  getRoutes() {
    return this.api.get<ResponseArray<PharmaCatalogItem>>('Pharma/routes');
  }

  searchSubstances(nombre: string) {
    return this.api.get<ResponseArray<PharmaSubstance>>(`Pharma/substances?nombre=${encodeURIComponent(nombre)}`);
  }

  getByProduct(productId: string) {
    return this.api.get<ResponseObject<ProductPharma>>(`Pharma/product/${productId}`);
  }

  /**
   * Guarda ficha y composición en un solo viaje: la composición se reemplaza
   * completa, así que enviarla por partes dejaría estados intermedios raros.
   */
  savePharma(productId: string, datos: ProductPharma) {
    return this.api.put<ResponseObject<boolean>>(`Pharma/product/${productId}`, {
      FormId: datos.FormId || null,
      RouteId: datos.RouteId || null,
      Presentation: datos.Presentation || null,
      DosageReference: datos.DosageReference || null,
      ProductType: datos.ProductType || null,
      SanitaryRegistry: datos.SanitaryRegistry || null,
      SanitaryRegistryExpiry: datos.SanitaryRegistryExpiry || null,
      Components: datos.Components
        .filter(c => c.SubstanceId || c.SubstanceName.trim())
        .map(c => ({
          SubstanceId: c.SubstanceId || null,
          SubstanceName: c.SubstanceName.trim(),
          ConcentrationValue: c.ConcentrationValue,
          ConcentrationUnit: c.ConcentrationUnit || null,
          IsActiveIngredient: c.IsActiveIngredient,
        })),
    });
  }

  /**
   * Prospecto del producto. Se pide aparte de la ficha a propósito: son varios
   * KB y la mayoría de los productos no lo tiene, así que solo se trae cuando
   * alguien lo va a leer o editar.
   */
  getLeaflet(productId: string) {
    return this.api.get<ResponseObject<string>>(`Pharma/product/${productId}/leaflet`);
  }

  /** Guardar vacío borra el prospecto. */
  saveLeaflet(productId: string, content: string) {
    return this.api.put<ResponseObject<boolean>>(`Pharma/product/${productId}/leaflet`, { Content: content });
  }

  /** Alternativa definida a mano: la comercial, la más económica. */
  addAlternative(productId: string, alternativeId: string, reason: string) {
    return this.api.post<ResponseObject<boolean>>(`Pharma/product/${productId}/alternatives`,
      { AlternativeId: alternativeId, Reason: reason });
  }

  removeAlternative(productId: string, alternativeId: string) {
    return this.api.del<ResponseObject<boolean>>(`Pharma/product/${productId}/alternatives/${alternativeId}`);
  }

  /**
   * Alternativas de un producto: las deducidas por composición y las definidas
   * a mano, en una sola lista. Cada una viene marcada con `IsManual`, porque no
   * son lo mismo: una es intercambiable y la otra es una sugerencia.
   */
  getEquivalents(productId: string) {
    return this.api.get<ResponseArray<ProductEquivalent>>(`Pharma/product/${productId}/equivalents`);
  }

  /**
   * En qué fichas se está ofreciendo este producto. Es la vuelta de una
   * relación que se guarda en un solo sentido.
   */
  getSuggestedIn(productId: string) {
    return this.api.get<ResponseArray<ProductEquivalent>>(`Pharma/product/${productId}/suggested-in`);
  }

  /**
   * Fija el orden en que se ofrecen. Una lista vacía devuelve el control al
   * orden automático: disponibilidad primero y después precio.
   */
  setAlternativesOrder(productId: string, alternativeIds: string[]) {
    return this.api.put<ResponseObject<boolean>>(`Pharma/product/${productId}/alternatives/order`,
      { AlternativeIds: alternativeIds });
  }
}
