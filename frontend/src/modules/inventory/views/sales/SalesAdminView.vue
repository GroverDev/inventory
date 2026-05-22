<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Ventas</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>VENTAS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Filtros -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Inicio</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.dateInitial" />
              </div>
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Fin</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.dateEnd" />
              </div>
              <div class="col-12 col-md-3">
                <button class="btn btn-primary btn-sm w-100" @click="getSalesData">
                  <span class="fal fa-search me-1"></span>Buscar
                </button>
              </div>
            </div>

            <!-- Contador -->
            <div v-if="sales.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ sales.length }}</strong> venta(s) encontrada(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="sales.length === 0" class="text-center py-5">
              <i class="fal fa-receipt fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-0">Seleccione un rango de fechas y haga clic en Buscar</p>
            </div>

            <template v-else>
              <!-- Tabla desktop -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Fecha</th>
                      <th>Cliente</th>
                      <th class="text-end">Subtotal</th>
                      <th class="text-end">Descuentos</th>
                      <th class="text-end">Total</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(sale, index) in sales" :key="index">
                      <td>{{ formatDate(sale.SaleDate) }}</td>
                      <td class="fw-semibold">{{ sale.CustomerName }}</td>
                      <td class="text-end">{{ formatCurrency(sale.Subtotal) }}</td>
                      <td class="text-end text-danger">{{ formatCurrency(sale.TotalDiscounts) }}</td>
                      <td class="text-end fw-semibold">{{ formatCurrency(sale.Total) }}</td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Ver detalle"
                          @click="viewDetail(sale.Id)"
                        >
                          <span class="fal fa-eye"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeSale(sale.Id)"
                        >
                          <span class="fal fa-trash-alt"></span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                  <tfoot>
                    <tr class="fw-bold table-light">
                      <td colspan="4" class="text-end">TOTAL PERÍODO</td>
                      <td class="text-end">{{ formatCurrency(totalPeriod) }}</td>
                      <td></td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              <!-- Cards móvil -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12" v-for="(sale, index) in sales" :key="index">
                    <div class="card">
                      <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <h6 class="card-title mb-0">{{ sale.CustomerName }}</h6>
                          <span class="fw-bold">{{ formatCurrency(sale.Total) }}</span>
                        </div>
                        <small class="text-muted d-block mb-2">
                          <i class="fal fa-calendar me-1"></i>{{ formatDate(sale.SaleDate) }}
                        </small>
                        <div class="d-flex gap-2">
                          <button type="button" class="btn btn-outline-primary btn-sm flex-fill"
                            @click="viewDetail(sale.Id)">
                            <span class="fal fa-eye me-1"></span>Ver Detalle
                          </button>
                          <button type="button" class="btn btn-outline-danger btn-sm"
                            @click="removeSale(sale.Id)">
                            <span class="fal fa-trash-alt"></span>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </template>

          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import useSales from '@/modules/inventory/composables/useSales';
import type { Sale } from '@/modules/inventory/models/sale.model';
import utils from '@/utils/msg';

const sales = ref<Sale[]>([]);
const { getSales, deleteSale } = useSales();
const router = useRouter();

const today = new Date().toISOString().split('T')[0];
const firstOfMonth = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0];

const filtro = ref({ dateInitial: firstOfMonth, dateEnd: today });

const totalPeriod = computed(() => +sales.value.reduce((s, v) => s + v.Total, 0).toFixed(2));

const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  val?.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' }) ?? 'Bs. 0.00';

const getSalesData = async () => {
  const { Data } = await getSales(filtro.value.dateInitial, filtro.value.dateEnd);
  sales.value = Data ?? [];
};

const viewDetail = (id: string) => router.push({ name: 'sale-detail', params: { id } });

const removeSale = async (id: string) => {
  const ok = await utils.showMessageQuestion('¿Desea eliminar esta venta?');
  if (ok) {
    const { ok: deleted } = await deleteSale(id);
    if (deleted) {
      await utils.showMessageModal({ Description: 'La venta se eliminó correctamente.', MessageType: 'success' });
      await getSalesData();
    }
  }
};

onMounted(getSalesData);
</script>

<style scoped></style>
