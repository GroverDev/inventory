<template>
  <div>
    <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
      <i class="fal fa-store-alt me-1"></i> Precio y mínimo por sucursal
      <small class="fw-normal ms-1">(opcional)</small>
    </h6>
    <p class="small text-muted mb-2">
      Deje en blanco para usar los valores base de arriba. Complete solo las sucursales que venden a otro
      precio o necesitan otro mínimo de reposición.
    </p>

    <div v-if="rows.length === 0" class="text-center py-2">
      <small class="text-muted">Cargando sucursales...</small>
    </div>
    <div v-else class="table-responsive">
      <table class="table table-sm align-middle mb-2">
        <thead>
          <tr>
            <th>Sucursal</th>
            <th class="text-center">Stock</th>
            <th style="width:11rem">Precio (Bs.)</th>
            <th style="width:9rem">Mínimo</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in rows" :key="r.BranchId">
            <td>
              {{ r.BranchName }}
              <span v-if="r.BranchId === authStore.getUser?.BranchId" class="badge bg-info ms-1">Actual</span>
            </td>
            <td class="text-center">{{ r.Stock }}</td>
            <td>
              <input type="number" min="0" step="0.01" class="form-control form-control-sm text-end"
                :placeholder="`base ${r.BaseSalePrice.toFixed(2)}`" :disabled="!canUpdate"
                v-model="r.SalePriceText" />
            </td>
            <td>
              <input type="number" min="0" step="1" class="form-control form-control-sm text-end"
                :placeholder="`base ${r.BaseMinReorderQuantity}`" :disabled="!canUpdate"
                v-model="r.MinText" />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <button v-if="canUpdate && rows.length > 0" type="button" class="btn btn-sm btn-outline-primary mb-3"
      :disabled="saving" @click="save">
      <span class="fal fa-save me-1"></span>Guardar precios por sucursal
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import { usePermissions } from '@/modules/common/composables/usePermissions';
import utils from '@/utils/msg';

interface BranchSetting {
  BranchId: string;
  BranchName: string;
  SalePrice: number | null;
  MinReorderQuantity: number | null;
  BaseSalePrice: number;
  BaseMinReorderQuantity: number;
  Stock: number;
}

/** Fila editable: los inputs trabajan con texto para distinguir "vacío" de 0. */
interface Row extends BranchSetting {
  SalePriceText: string | number;
  MinText: string | number;
}

const props = defineProps<{ productId: string }>();

const { get, put } = useApi();
const authStore = useAuthStore();
const { can } = usePermissions();
const canUpdate = computed(() => can('products-admin', 'update'));

const rows = ref<Row[]>([]);
const saving = ref(false);

onMounted(() => load());

const load = async () => {
  const { ok, Data } = await get<ResponseArray<BranchSetting>>(`Product/${props.productId}/branch-settings`);
  if (!ok) return;
  rows.value = Data.map(s => ({
    ...s,
    SalePriceText: s.SalePrice ?? '',
    MinText: s.MinReorderQuantity ?? '',
  }));
};

/** '' o null = sin excepción; cualquier otro valor, número. */
const toNumber = (v: string | number): number | null =>
  v === '' || v === null || v === undefined ? null : Number(v);

const save = async () => {
  const payload = rows.value.map(r => ({
    branchId: r.BranchId,
    salePrice: toNumber(r.SalePriceText),
    minReorderQuantity: toNumber(r.MinText),
  }));

  if (payload.some(p => (p.salePrice !== null && (isNaN(p.salePrice) || p.salePrice < 0))
                     || (p.minReorderQuantity !== null && (!Number.isInteger(p.minReorderQuantity) || p.minReorderQuantity < 0)))) {
    await utils.showMessageModal({ Description: 'Los precios no pueden ser negativos y el mínimo debe ser un número entero.', MessageType: 'warning' });
    return;
  }

  saving.value = true;
  const { ok } = await put<ResponseObject<boolean>>(`Product/${props.productId}/branch-settings`, payload);
  saving.value = false;
  if (!ok) return;

  await utils.showMessageModal({ Description: 'Precios por sucursal guardados.', MessageType: 'success' });
  await load();
};
</script>
