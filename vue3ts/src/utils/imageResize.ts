/**
 * Prepara una foto antes de subirla: las del celular pesan 5-10 MB y el servidor
 * las reduce a 800 px de todos modos, así que se bajan acá y se ahorra la subida.
 */

export const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
/** Mismo tope que el servidor (5 MB). */
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

const MAX_SIDE = 1600;
// Por debajo de esto no vale la pena tocarla.
const SKIP_BELOW_BYTES = 500 * 1024;

/**
 * Devuelve el archivo listo para subir: reducido si era grande, tal cual si ya
 * era liviano o si el navegador no pudo procesarlo (el servidor lo valida igual).
 */
export const prepareImage = async (file: File): Promise<Blob> => {
  if (file.size <= SKIP_BELOW_BYTES) return file;

  try {
    // 'from-image' aplica la orientación EXIF, para que la foto no salga de costado.
    const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });
    const scale = Math.min(1, MAX_SIDE / Math.max(bitmap.width, bitmap.height));
    const width = Math.round(bitmap.width * scale);
    const height = Math.round(bitmap.height * scale);

    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    canvas.getContext('2d')!.drawImage(bitmap, 0, 0, width, height);
    bitmap.close();

    const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/jpeg', 0.85));
    // Si por algún motivo pesa más que el original, se prefiere el original.
    return blob && blob.size < file.size ? blob : file;
  } catch {
    return file;
  }
};
