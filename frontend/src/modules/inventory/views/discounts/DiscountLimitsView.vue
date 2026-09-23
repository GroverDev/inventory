<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Ventas</li>
        <li class="breadcrumb-item active" aria-current="page">Límites de Descuento</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Límites del <span class="fw-300"><i>DESCUENTO MANUAL</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">
            <ul class="small text-muted mb-3">
              <li><strong>Tope del cajero:</strong> por encima, el punto de venta pide la autorización de un supervisor.</li>
              <li><strong>Tope máximo:</strong> nadie lo supera, ni un administrador ni con autorización. Vacío = sin tope.</li>
              <li>Cada sucursal usa su valor si lo tiene; si no, el de la empresa. Deje en blanco para heredar.</li>
              <li>Los descuentos del catálogo no están sujetos a estos topes: ya los aprobó quien los dio de alta.</li>
            </ul>

            <div v-if="levels.length === 0" class="text-center py-4">
              <small class="text-muted">Cargando...</small>
            </div>
            <div v-else class="table-responsive">
              <table class="table table-sm align-middle mb-3">
                <thead>
                  <tr>
                    <th rowspan="2" class="align-bottom">Nivel</th>
                    <th colspan="2" class="text-center border-bottom-0">Tope del cajero</th>
                    <th colspan="2" class="text-center border-bottom-0">Tope máximo</th>
                  </tr>
                  <tr>
                    <th class="text-center" style="width:9rem">%</th>
                    <th class="text-center" style="width:9rem">Bs.</th>
                    <th class="text-center" style="width:9rem">%</th>
                    <th class="text-center" style="width:9rem">Bs.</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(l, i) in levels" :key="l.BranchId ?? 'empresa'" :class="{ 'table-active': i === 0 }">
                    <td>
                      <span v-if="i === 0" class="fw-semibold">Toda la empresa</span>
                      <span v-else class="ps-3">{{ l.BranchName }}</span>
                    </td>
                    <td v-for="campo in campos" :key="campo.key">
                      <input type="number" min="0" :max="campo.pct ? 100 : undefined" step="0.01"
                        class="form-control form-control-sm text-end" :disabled="!canUpdate"
                        :placeholder="heredado(i, campo.key)" v-model="l[campo.key]" />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <button v-if="canUpdate && levels.length" type="button" class="btn btn-sm btn-primary"
              :disabled="saving" @click="save">
              <span class="fal fa-save me-1"></span>Guardar
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import { usePermissions } from '@/modules/common/composables/usePermissions';
import utils from '@/utils/msg';

type Campo = 'CashierMaxPct' | 'CashierMaxAmount' | 'GeneralMaxPct' | 'GeneralMaxAmount';

/** Los inputs trabajan con texto para distinguir "vacío" (hereda) de 0. */
interface Level {
  BranchId: string | null;
  BranchName: string;
  CashierMaxPct: number | string | null;
  CashierMaxAmount: number | string | null;
  GeneralMaxPct: number | string | null;
  GeneralMaxAmount: number | string | null;
}

const campos: { key: Campo; pct: boolean }[] = [
  { key: 'CashierMaxPct', pct: true },
  { key: 'CashierMaxAmount', pct: false },
  { key: 'GeneralMaxPct', pct: true },
  { key: 'GeneralMaxAmount', pct: false },
];

/** Default del cajero cuando nadie lo configuró: el de appsettings del servidor. */
const DEFAULT_CAJERO: Record<string, string> = { CashierMaxPct: '15', CashierMaxAmount: '50' };

const { get, put } = useApi();
const { can } = usePermissions();
const canUpdate = computed(() => can('discount-limits', 'update'));

const levels = ref<Level[]>([]);
const saving = ref(false);

onMounted(() => load());

const load = async () => {
  const { ok, Data } = await get<ResponseArray<Level>>('Settings/discount-limits');
  if (!ok) return;
  // La API omite los campos nulos: se normalizan a '' para los inputs.
  levels.value = Data.map(l => ({
    BranchId: l.BranchId ?? null,
    BranchName: l.BranchName ?? '',
    CashierMaxPct: l.CashierMaxPct ?? '',
    CashierMaxAmount: l.CashierMaxAmount ?? '',
    GeneralMaxPct: l.GeneralMaxPct ?? '',
    GeneralMaxAmount: l.GeneralMaxAmount ?? '',
  }));
};

const vacio = (v: unknown) => v === '' || v === null || v === undefined;

/** Lo que rige si el campo queda vacío: el de la empresa, o el default. */
const heredado = (i: number, campo: Campo): string => {
  const empresa = levels.value[0]?.[campo];
  if (i > 0 && !vacio(empresa)) return `empresa: ${empresa}`;
  return DEFAULT_CAJERO[campo] ? `default: ${DEFAULT_CAJERO[campo]}` : 'sin tope';
};

const toNumber = (v: unknown): number | null => (vacio(v) ? null : Number(v));

const save = async () => {
  const payload = levels.value.map(l => ({
    branchId: l.BranchId,
    cashierMaxPct: toNumber(l.CashierMaxPct),
    cashierMaxAmount: toNumber(l.CashierMaxAmount),
    generalMaxPct: toNumber(l.GeneralMaxPct),
    generalMaxAmount: toNumber(l.GeneralMaxAmount),
    branchName: l.BranchName,
  }));

  saving.value = true;
  const { ok } = await put<ResponseObject<boolean>>('Settings/discount-limits', payload);
  saving.value = false;
  if (!ok) return;

  await utils.showMessageModal({
    Description: 'Límites guardados. Los puntos de venta abiertos los toman al recargarse.',
    MessageType: 'success',
  });
  await load();
};
</script>

<style scoped></style>
