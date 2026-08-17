import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type {
  PharmaCatalogItem, PharmaSubstance, ProductPharma, ProductEquivalent,
} from '@/modules/inventory/models/pharma.model';

const { get, put, post, del } = useApi();

const usePharma = () => {

  const getForms = async (): Promise<ResponseArray<PharmaCatalogItem>> =>
    await get<ResponseArray<PharmaCatalogItem>>('Pharma/forms');

  const getRoutes = async (): Promise<ResponseArray<PharmaCatalogItem>> =>
    await get<ResponseArray<PharmaCatalogItem>>('Pharma/routes');

  const searchSubstances = async (nombre: string): Promise<ResponseArray<PharmaSubstance>> =>
    await get<ResponseArray<PharmaSubstance>>(`Pharma/substances?nombre=${encodeURIComponent(nombre)}`);

  const getByProduct = async (productId: string): Promise<ResponseObject<ProductPharma>> =>
    await get<ResponseObject<ProductPharma>>(`Pharma/product/${productId}`);

  /**
   * Guarda ficha y composición en un solo viaje: la composición se reemplaza
   * completa, así que enviarla por partes dejaría estados intermedios raros.
   */
  const savePharma = async (productId: string, datos: ProductPharma): Promise<ResponseObject<boolean>> =>
    await put<ResponseObject<boolean>>(`Pharma/product/${productId}`, {
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

  /**
   * Prospecto del producto. Se pide aparte de la ficha a propósito: son varios
   * KB y la mayoría de los productos no lo tiene, así que solo se trae cuando
   * alguien lo va a leer o editar.
   */
  const getLeaflet = async (productId: string): Promise<ResponseObject<string>> =>
    await get<ResponseObject<string>>(`Pharma/product/${productId}/leaflet`);

  /** Guardar vacío borra el prospecto. */
  const saveLeaflet = async (productId: string, content: string): Promise<ResponseObject<boolean>> =>
    await put<ResponseObject<boolean>>(`Pharma/product/${productId}/leaflet`, { Content: content });

  /** Alternativa definida a mano: la comercial, la más económica. */
  const addAlternative = async (productId: string, alternativeId: string, reason: string): Promise<ResponseObject<boolean>> =>
    await post<ResponseObject<boolean>>(`Pharma/product/${productId}/alternatives`,
      { AlternativeId: alternativeId, Reason: reason });

  const removeAlternative = async (productId: string, alternativeId: string): Promise<ResponseObject<boolean>> =>
    await del<ResponseObject<boolean>>(`Pharma/product/${productId}/alternatives/${alternativeId}`);

  /**
   * Alternativas de un producto: las deducidas por composición y las definidas
   * a mano, en una sola lista. Cada una viene marcada con `IsManual`, porque no
   * son lo mismo — una es intercambiable y la otra es una sugerencia.
   */
  const getEquivalents = async (productId: string): Promise<ResponseArray<ProductEquivalent>> =>
    await get<ResponseArray<ProductEquivalent>>(`Pharma/product/${productId}/equivalents`);

  /**
   * En qué fichas se está ofreciendo este producto. Es la vuelta de una
   * relación que se guarda en un solo sentido, y sin ella un producto puede
   * estar sugerido en diez lados sin que se note desde el suyo.
   */
  const getSuggestedIn = async (productId: string): Promise<ResponseArray<ProductEquivalent>> =>
    await get<ResponseArray<ProductEquivalent>>(`Pharma/product/${productId}/suggested-in`);

  /**
   * Fija el orden en que se ofrecen. Una lista vacía devuelve el control al
   * orden automático: disponibilidad primero y después precio.
   */
  const setAlternativesOrder = async (productId: string, alternativeIds: string[]): Promise<ResponseObject<boolean>> =>
    await put<ResponseObject<boolean>>(`Pharma/product/${productId}/alternatives/order`,
      { AlternativeIds: alternativeIds });

  return { getForms, getRoutes, searchSubstances, getByProduct, savePharma, getLeaflet, saveLeaflet, getEquivalents, getSuggestedIn, addAlternative, removeAlternative, setAlternativesOrder };
};

export default usePharma;
