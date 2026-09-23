<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Caja</li>
        <li class="breadcrumb-item active" aria-current="page">Configuración de Cierre</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Configuración del <span class="fw-300"><i>CIERRE DE CAJA</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">
            <ul class="small text-muted mb-3">
              <li>Al cerrar, el cajero declara lo que tiene <strong>sin ver lo esperado</strong> (conteo a ciegas).
                Recién al confirmar ve la diferencia.</li>
              <li>El efectivo se arquea siempre. Tarjeta y QR, si los marca: se declaran con el cierre de lote del
                datáfono y con lo recibido en la cuenta.</li>
              <li>Si en algún medio la diferencia supera el monto de abajo, el cierre exige una observación.
                Los intentos rechazados quedan registrados en el turno.</li>
              <li>Una diferencia mayor, o demasiados intentos, pueden exigir además la autorización de un supervisor.</li>
            </ul>

            <h6 class="text-muted border-bottom pb-2 mb-2">Medios que se arquean</h6>
            <div class="table-responsive mb-3">
              <table class="table table-sm align-middle mb-0">
                <tbody>
                  <tr v-for="m in methods" :key="m.Id">
                    <td><i :class="m.IconCss" class="me-2 text-muted"></i>{{ m.Name }}</td>
                    <td class="text-end">
                      <div class="form-check form-switch d-inline-block mb-0">
                        <input class="form-check-input" type="checkbox" :id="`count-${m.Id}`"
                          :disabled="m.AffectsCash || !canUpdate" v-model="m.RequiresCount" />
                        <label class="form-check-label small" :for="`count-${m.Id}`">
                          {{ m.AffectsCash ? 'Siempre (entra al cajón)' : m.RequiresCount ? 'Se arquea' : 'No se arquea' }}
                        </label>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <h6 class="text-muted border-bottom pb-2 mb-2">Observación obligatoria</h6>
            <div class="row mb-3">
              <div class="col-12 col-sm-6 col-md-4">
                <label class="form-label small">Si la diferencia de un medio supera</label>
                <div class="input-group input-group-sm">
                  <span class="input-group-text">Bs.</span>
                  <input type="number" min="0" step="0.01" class="form-control text-end"
                    :disabled="!canUpdate" v-model.number="noteThreshold" />
                </div>
              </div>
            </div>

            <h6 class="text-muted border-bottom pb-2 mb-2">Autorización de supervisor</h6>
            <p class="small text-muted mb-2">
              Un cajero no puede cerrar solo: tiene que ingresar un supervisor en su punto de venta.
              Quien no es cajero se autoriza a sí mismo. Vacío = no se exige.
            </p>
            <div class="row mb-3 g-2">
              <div class="col-12 col-sm-6 col-md-4">
                <label class="form-label small">Si la diferencia de un medio supera</label>
                <div class="input-group input-group-sm">
                  <span class="input-group-text">Bs.</span>
                  <input type="number" min="0" step="0.01" class="form-control text-end" placeholder="Sin tope"
                    :disabled="!canUpdate" v-model="supervisorThreshold" />
                </div>
              </div>
              <div class="col-12 col-sm-6 col-md-4">
                <label class="form-label small">Después de estos intentos rechazados</label>
                <input type="number" min="1" step="1" class="form-control form-control-sm text-end" placeholder="Sin límite"
                  :disabled="!canUpdate" v-model="maxAttempts" />
              </div>
            </div>

            <h6 class="text-muted border-bottom pb-2 mb-2">Conteo del efectivo</h6>
            <div class="form-check form-switch mb-1">
              <input class="form-check-input" type="checkbox" id="require-denominations"
                :disabled="!canUpdate" v-model="requireDenominations" />
              <label class="form-check-label small" for="require-denominations">
                Contar por billetes y monedas (obligatorio)
              </label>
            </div>
            <p class="small text-muted mb-3">
              Si no es obligatorio, el cajero puede usarlo igual desde el botón junto al efectivo.
            </p>

            <div v-if="supervisorThreshold !== '' || maxAttempts !== '' || requireDenominations"
              class="alert alert-warning py-2 small">
              <i class="fal fa-mobile me-1"></i>
              La app móvil todavía no pide supervisor ni billetes: cuando el cierre los necesite, esa caja se
              cierra desde la web.
            </div>

            <button v-if="canUpdate" type="button" class="btn btn-sm btn-primary" :disabled="saving" @click="save">
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
import type { ResponseObject } from '@/modules/common/models/response.model';
import type { PaymentMethod } from '@/modules/inventory/models/paymentMethod.model';
import type { CashCloseSettings } from '@/modules/inventory/models/cashSession.model';
import { usePermissions } from '@/modules/common/composables/usePermissions';
import utils from '@/utils/msg';

const { get, put } = useApi();
const { can } = usePermissions();
const canUpdate = computed(() => can('cash-close-settings', 'update'));

const methods = ref<PaymentMethod[]>([]);
const noteThreshold = ref<number>(10);
// '' = sin tope / sin límite (se guarda null).
const supervisorThreshold = ref<number | ''>('');
const maxAttempts = ref<number | ''>('');
const requireDenominations = ref(false);
const optional = (v: number | '') => (v === '' || v === null ? null : Number(v));
const saving = ref(false);

onMounted(async () => {
  const { ok, Data } = await get<ResponseObject<CashCloseSettings & { Methods: PaymentMethod[] }>>('Settings/cash-close');
  if (!ok) return;
  noteThreshold.value = Data.NoteThreshold;
  supervisorThreshold.value = Data.SupervisorThreshold ?? '';
  maxAttempts.value = Data.MaxAttempts ?? '';
  requireDenominations.value = Data.RequireDenominations;
  methods.value = Data.Methods;
});

const save = async () => {
  if (!(noteThreshold.value >= 0)) {
    await utils.showMessageModal({ Description: 'El monto no puede ser negativo.', MessageType: 'warning' });
    return;
  }
  const tope = optional(supervisorThreshold.value);
  if (tope !== null && tope < noteThreshold.value) {
    await utils.showMessageModal({
      Description: 'El monto que pide supervisor no puede ser menor al que pide observación.', MessageType: 'warning' });
    return;
  }
  const intentos = optional(maxAttempts.value);
  if (intentos !== null && (!Number.isInteger(intentos) || intentos < 1)) {
    await utils.showMessageModal({ Description: 'Los intentos tienen que ser un número entero desde 1.', MessageType: 'warning' });
    return;
  }
  saving.value = true;
  const { ok } = await put<ResponseObject<boolean>>('Settings/cash-close', {
    noteThreshold: noteThreshold.value,
    supervisorThreshold: tope,
    maxAttempts: intentos,
    requireDenominations: requireDenominations.value,
    methods: methods.value.map(m => ({ id: m.Id, requiresCount: m.RequiresCount })),
  });
  saving.value = false;
  if (ok)
    await utils.showMessageModal({
      Description: 'Configuración guardada. Los puntos de venta la toman al recargarse.',
      MessageType: 'success',
    });
};
</script>

<style scoped></style>
