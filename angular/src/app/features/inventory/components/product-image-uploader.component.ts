import { Component, OnDestroy, computed, inject, input, output, signal } from '@angular/core';

import { MsgService } from '../../../core/services/msg.service';
import { ALLOWED_IMAGE_TYPES, MAX_IMAGE_BYTES, prepareImage } from '../../../core/utils/image-resize';
import { mediaUrl } from '../../../core/utils/media-url';
import { ProductService } from '../products/product.service';
import { ImageLightboxComponent } from './image-lightbox.component';
import { ProductThumbComponent } from './product-thumb.component';

/** Subir, cambiar o quitar la imagen de un producto (o elegirla antes de que el producto exista). */
@Component({
  selector: 'app-product-image-uploader',
  imports: [ProductThumbComponent, ImageLightboxComponent],
  template: `
    <div class="d-flex align-items-start gap-3">
      <div class="flex-shrink-0 rounded overflow-hidden border"
           style="width: 140px; height: 140px"
           [style.cursor]="hasImage() ? 'zoom-in' : null"
           [attr.role]="hasImage() ? 'button' : null"
           [title]="hasImage() ? 'Clic para ampliar' : ''"
           (click)="hasImage() && ampliada.set(true)">
        @if (previewUrl()) {
          <img [src]="previewUrl()" alt="Vista previa" class="w-100 h-100" style="object-fit: cover">
        } @else {
          <app-product-thumb [path]="imagePath()" [size]="140" [full]="true" alt="Imagen del producto" />
        }
      </div>
      <app-image-lightbox
        [open]="ampliada()"
        [src]="previewUrl() || fullUrl()"
        [placeholder]="previewUrl() ? undefined : thumbUrl()"
        (close)="ampliada.set(false)" />

      <div class="flex-grow-1">
        <input #fileInput type="file" class="d-none" accept="image/jpeg,image/png,image/webp" (change)="onFileChosen($event)">
        <!-- Algunos celulares no ofrecen la cámara en el selector normal: con
             capture se abre directo. En escritorio se ignora. -->
        <input #cameraInput type="file" class="d-none" accept="image/*" capture="environment" (change)="onFileChosen($event)">

        @if (!readonly()) {
        <div class="d-flex flex-wrap gap-2 mb-2">
          <button type="button" class="btn btn-sm btn-outline-primary" [disabled]="busy()" (click)="fileInput.click()">
            <i class="ri-upload-2-line align-middle me-1"></i>{{ hasImage() ? 'Cambiar imagen' : 'Subir imagen' }}
          </button>
          <button type="button" class="btn btn-sm btn-outline-primary" [disabled]="busy()" (click)="cameraInput.click()">
            <i class="ri-camera-line align-middle me-1"></i>Tomar foto
          </button>
          @if (hasImage()) {
          <button type="button" class="btn btn-sm btn-outline-danger" [disabled]="busy()" (click)="remove()">
            <i class="ri-delete-bin-line align-middle me-1"></i>Quitar
          </button>
          }
        </div>
        }

        <small class="text-muted d-block">JPG, PNG o WebP. Se reduce automáticamente; máximo 5 MB.</small>
        @if (isNew() && pendingName()) {
        <small class="text-muted d-block"><i class="ri-time-line me-1"></i>«{{ pendingName() }}» se subirá al guardar el producto.</small>
        } @else if (isNew()) {
        <small class="text-muted d-block">Puede elegirla ahora; se sube cuando guarde el producto.</small>
        }
        @if (error()) {<small class="text-danger d-block mt-1">{{ error() }}</small>}
      </div>
    </div>
  `,
})
export class ProductImageUploaderComponent implements OnDestroy {
  private readonly products = inject(ProductService);
  private readonly msg = inject(MsgService);

  /** '0' mientras el producto no se ha creado. */
  readonly productId = input.required<string>();
  readonly imagePath = input<string | null>(null);
  readonly readonly = input(false);

  /** Con producto ya creado: la imagen se subió y esta es su nueva ruta. */
  readonly uploaded = output<string>();
  readonly removed = output<void>();
  /** Con producto nuevo: el archivo elegido, para que el padre lo suba tras crearlo. */
  readonly picked = output<{ file: Blob | null; name: string }>();

  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly previewUrl = signal('');
  protected readonly pendingName = signal('');
  protected readonly ampliada = signal(false);

  protected readonly isNew = computed(() => this.productId() === '0');
  protected readonly hasImage = computed(() => !!this.imagePath() || !!this.previewUrl());
  protected readonly fullUrl = computed(() => mediaUrl(this.imagePath()));
  protected readonly thumbUrl = computed(() => mediaUrl(this.imagePath(), true));

  ngOnDestroy() {
    this.setPreview(null);
  }

  private setPreview(blob: Blob | null) {
    if (this.previewUrl()) URL.revokeObjectURL(this.previewUrl());
    this.previewUrl.set(blob ? URL.createObjectURL(blob) : '');
  }

  protected async onFileChosen(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // permite volver a elegir el mismo archivo
    if (!file) return;

    this.error.set('');
    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      this.error.set('Formato no permitido. Use JPG, PNG o WebP.');
      return;
    }

    this.busy.set(true);
    try {
      const blob = await prepareImage(file);
      if (blob.size > MAX_IMAGE_BYTES) {
        this.error.set('La imagen pesa más de 5 MB.');
        return;
      }

      if (this.isNew()) {
        // Todavía no hay producto al que colgarla: se muestra y el padre la sube
        // cuando el producto exista.
        this.setPreview(blob);
        this.pendingName.set(file.name);
        this.picked.emit({ file: blob, name: file.name });
        return;
      }

      // La API avisa los errores por su cuenta (formato, tamaño...) con el modal.
      const { ok, Data: path } = await this.products.uploadImage(this.productId(), blob, file.name);
      if (ok) {
        this.setPreview(null);
        this.uploaded.emit(path);
      }
    } finally {
      this.busy.set(false);
    }
  }

  protected async remove() {
    if (this.isNew()) {
      this.setPreview(null);
      this.pendingName.set('');
      this.picked.emit({ file: null, name: '' });
      return;
    }
    if (!(await this.msg.showMessageQuestion('¿Desea quitar la imagen del producto?'))) return;

    this.busy.set(true);
    try {
      const { ok } = await this.products.deleteImage(this.productId());
      if (ok) this.removed.emit();
    } finally {
      this.busy.set(false);
    }
  }
}
