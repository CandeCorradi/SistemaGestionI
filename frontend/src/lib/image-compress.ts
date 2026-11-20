/**
 * Optimización de imágenes antes de subirlas al storage (AGENTS.md §63).
 *
 * El storage conserva el archivo tal cual; la subida es el único punto
 * donde se puede reducir el peso. Redimensiona a un lado máximo y
 * comprime iterativamente hasta alcanzar el peso objetivo para que las
 * miniaturas del catálogo y las galerías carguen rápido.
 *
 * - Los GIF se envían tal cual: el canvas no conserva la animación.
 * - Si el archivo ya cumple el peso, se sube sin modificar.
 * - Los PNG pasan a WebP (conservan transparencia y pesan menos);
 *   si el navegador no soporta WebP, `toBlob` cae a PNG y se sube
 *   igual (mejor esfuerzo).
 */

const MAX_DIMENSION = 1600;
const DEFAULT_MAX_BYTES = 900 * 1024;

const PASS_THROUGH_TYPES = ['image/gif'];

interface EncodingAttempt {
  maxDimension: number;
  qualities: number[];
}

const ATTEMPTS: EncodingAttempt[] = [
  { maxDimension: MAX_DIMENSION, qualities: [0.82, 0.7, 0.6, 0.5, 0.4] },
  { maxDimension: Math.round(MAX_DIMENSION / 2), qualities: [0.6, 0.4] },
];

const EXTENSION_BY_TYPE: Record<string, string> = {
  'image/jpeg': '.jpg',
  'image/png': '.png',
  'image/webp': '.webp',
  'image/gif': '.gif',
};

/** Devuelve un File pesado a lo sumo `maxBytes` (mejor esfuerzo). */
export async function compressImageForUpload(
  file: File,
  maxBytes: number = DEFAULT_MAX_BYTES
): Promise<File> {
  if (PASS_THROUGH_TYPES.includes(file.type) || file.size <= maxBytes) {
    return file;
  }

  const image = await loadImageElement(file);
  // Los PNG se re-codifican como WebP para conservar la transparencia
  // con mejor ratio; el resto (JPG/WEBP) va a JPEG.
  const targetMime = file.type === 'image/png' ? 'image/webp' : 'image/jpeg';

  let best: { blob: Blob; fileName: string } | null = null;

  for (const { maxDimension, qualities } of ATTEMPTS) {
    const size = scaleSize(image.naturalWidth, image.naturalHeight, maxDimension);
    for (const quality of qualities) {
      const blob = await encode(image, size, targetMime, quality);
      if (!blob) {
        continue;
      }
      const fileName = replaceExtension(file.name, EXTENSION_BY_TYPE[blob.type] ?? '.jpg');
      if (!best || blob.size < best.blob.size) {
        best = { blob, fileName };
      }
      if (blob.size <= maxBytes) {
        return new File([blob], fileName, { type: blob.type, lastModified: Date.now() });
      }
    }
  }

  if (!best) {
    // Sin canvas utilizable: subir el original y dejar que el backend
    // aplique su límite de 5 MB.
    return file;
  }
  return new File([best.blob], best.fileName, {
    type: best.blob.type,
    lastModified: Date.now(),
  });
}

async function loadImageElement(file: File): Promise<HTMLImageElement> {
  const url = URL.createObjectURL(file);
  try {
    const image = new Image();
    image.src = url;
    await image.decode();
    return image;
  } finally {
    URL.revokeObjectURL(url);
  }
}

function scaleSize(width: number, height: number, maxDimension: number): {
  width: number;
  height: number;
} {
  const longest = Math.max(width, height);
  if (longest <= maxDimension) {
    return { width, height };
  }
  const factor = maxDimension / longest;
  return { width: Math.round(width * factor), height: Math.round(height * factor) };
}

async function encode(
  image: HTMLImageElement,
  size: { width: number; height: number },
  mime: string,
  quality: number
): Promise<Blob | null> {
  const canvas = document.createElement('canvas');
  canvas.width = size.width;
  canvas.height = size.height;
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    return null;
  }
  ctx.drawImage(image, 0, 0, size.width, size.height);
  return new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, mime, quality));
}

function replaceExtension(fileName: string, extension: string): string {
  const dot = fileName.lastIndexOf('.');
  const base = dot > 0 ? fileName.slice(0, dot) : fileName;
  return `${base}${extension}`;
}
