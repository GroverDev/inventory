<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Ventas</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Detalle de Venta</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Detalle de <span class="fw-300"><i>Venta</i></span></h2>
        </div>
        <div class="panel-container show">

          <!-- Acciones -->
          <div class="panel-content pt-0">
            <div class="text-end">
              <button type="button" class="btn btn-danger btn-sm" @click="returnPage">
                <span class="fal fa-arrow-alt-to-left me-1"></span>Volver
              </button>
            </div>
          </div>

          <!-- Sin datos -->
          <div v-if="!sale.Id" class="panel-content text-center py-5">
            <i class="fal fa-spinner fa-spin fa-2x text-muted"></i>
            <p class="text-muted mt-2">Cargando...</p>
          </div>

          <template v-else>
            <!-- Cabecera -->
            <div class="panel-content pt-0">
              <h6 class="text-muted border-bottom pb-2 mb-3">
                <i class="fal fa-file-invoice me-1"></i> Datos de la Venta
              </h6>
              <div class="row g-3">
                <div class="col-12 col-md-5">
                  <label class="form-label text-muted small">Cliente</label>
                  <p class="fw-semibold mb-0">{{ sale.CustomerName || '—' }}</p>
                </div>
                <div class="col-6 col-md-3">
                  <label class="form-label text-muted small">Fecha de Venta</label>
                  <p class="mb-0">{{ formatDate(sale.SaleDate) }}</p>
                </div>
                <div class="col-6 col-md-2">
                  <label class="form-label text-muted small">Estado</label>
                  <p class="mb-0">
                    <span :class="sale.IsActive ? 'badge bg-success' : 'badge bg-secondary'">
                      {{ sale.IsActive ? 'Activa' : 'Inactiva' }}
                    </span>
                  </p>
                </div>
              </div>
            </div>

            <!-- Totales -->
            <div class="panel-content pt-0">
              <div class="row g-2">
                <div class="col-4">
                  <div class="card border-0 bg-light text-center py-2">
                    <small class="text-muted">Subtotal</small>
                    <div class="fw-semibold">{{ formatCurrency(sale.Subtotal) }}</div>
                  </div>
                </div>
                <div class="col-4">
                  <div class="card border-0 bg-light text-center py-2">
                    <small class="text-muted">Descuentos</small>
                    <div class="fw-semibold text-danger">{{ formatCurrency(sale.TotalDiscounts) }}</div>
                  </div>
                </div>
                <div class="col-4">
                  <div class="card border-0 bg-primary text-white text-center py-2">
                    <small>Total</small>
                    <div class="fw-bold fs-5">{{ formatCurrency(sale.Total) }}</div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Detalle de productos -->
            <div class="panel-content pt-0">
              <h6 class="text-muted border-bottom pb-2 mb-3">
                <i class="fal fa-list me-1"></i> Productos
              </h6>

              <div v-if="sale.Detail.length === 0" class="text-center py-3">
                <small class="text-muted">Sin detalle de productos.</small>
              </div>

              <template v-else>
                <!-- Tabla desktop -->
                <div class="d-none d-md-block">
                  <table class="table table-sm align-middle mb-0">
                    <thead class="table-light">
                      <tr>
                        <th>Producto</th>
                        <th class="text-center">Cantidad</th>
                        <th class="text-end">Precio Unit.</th>
                        <th class="text-end">Descuentos</th>
                        <th class="text-end">Total Línea</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(line, i) in sale.Detail" :key="i">
                        <td class="fw-semibold">{{ line.ProductName }}</td>
                        <td class="text-center">{{ line.Quantity }}</td>
                        <td class="text-end">{{ formatCurrency(line.UnitPrice) }}</td>
                        <td class="text-end text-danger">{{ formatCurrency(line.LineTotalDiscounts) }}</td>
                        <td class="text-end">{{ formatCurrency(line.LineTotal) }}</td>
                      </tr>
                    </tbody>
                    <tfoot>
                      <tr class="fw-bold table-light">
                        <td colspan="4" class="text-end">TOTAL</td>
                        <td class="text-end">{{ formatCurrency(sale.Total) }}</td>
                      </tr>
                    </tfoot>
                  </table>
                </div>

                <!-- Cards móvil -->
                <div class="d-md-none">
                  <div class="row g-2">
                    <div class="col-12" v-for="(line, i) in sale.Detail" :key="i">
                      <div class="card">
                        <div class="card-body py-2">
                          <div class="d-flex justify-content-between">
                            <span class="fw-semibold">{{ line.ProductName }}</span>
                            <span class="fw-bold">{{ formatCurrency(line.LineTotal) }}</span>
                          </div>
                          <small class="text-muted">
                            {{ line.Quantity }} × {{ formatCurrency(line.UnitPrice) }}
                            <span v-if="line.LineTotalDiscounts > 0" class="text-danger ms-1">
                              – {{ formatCurrency(line.LineTotalDiscounts) }} desc.
                            </span>
                          </small>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </template>
            </div>
          </template>

        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { Sale } from '@/modules/inventory/models/sale.model';
import useSales from '@/modules/inventory/composables/useSales';

const router = useRouter();
const route = useRoute();
const { getSaleById } = useSales();

const sale = ref(new Sale());

const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  val?.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' }) ?? 'Bs. 0.00';

onMounted(async () => {
  const id = route.params.id as string;
  if (id) {
    const { ok, Data } = await getSaleById(id);
    if (ok && Data) sale.value = Data;
  }
});

const returnPage = () => router.push({ name: 'sales-admin' });
</script>

<style scoped></style>
