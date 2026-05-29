<template>
  <div class="pos-page px-2 px-md-3 pt-2">

    <!-- ── Cabecera: volver + título + estado de caja ── -->
    <div class="pos-top-bar d-flex align-items-center mb-2 gap-2">
      <button class="btn btn-sm btn-outline-secondary" type="button" @click="router.back()">
        <i class="fal fa-arrow-left me-1"></i>Volver
      </button>
      <h6 class="mb-0 fw-semibold">Punto de Venta</h6>
      <div class="ms-auto d-flex align-items-center gap-2 flex-wrap">
        <!-- Caja abierta -->
        <template v-if="cashSession">
          <span class="badge bg-success-subtle text-success border border-success-subtle small d-none d-md-inline-flex align-items-center gap-1">
            <i class="fal fa-cash-register"></i>
            Caja: Bs. {{ formatNum(cashSession.OpeningAmount) }}
          </span>
          <button class="btn btn-sm btn-outline-secondary" @click="showMovementModal = true" title="Registrar gasto u otro movimiento">
            <i class="fal fa-receipt me-1"></i><span class="d-none d-md-inline">Gasto</span>
          </button>
          <button class="btn btn-sm btn-outline-danger" @click="showCloseCashModal = true" title="Cerrar caja">
            <i class="fal fa-lock me-1"></i><span class="d-none d-md-inline">Cerrar caja</span>
          </button>
        </template>
        <!-- Sin caja -->
        <template v-else>
          <span class="badge bg-warning-subtle text-warning border border-warning-subtle small">
            <i class="fal fa-exclamation-triangle me-1"></i>Sin caja abierta
          </span>
          <button class="btn btn-sm btn-success" @click="showOpenCashModal = true">
            <i class="fal fa-cash-register me-1"></i>Abrir caja
          </button>
        </template>
      </div>
    </div>

    <!-- ══ MODAL: Abrir Caja ══ -->
    <div v-if="showOpenCashModal" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered modal-sm">
        <div class="modal-content">
          <div class="modal-header py-2">
            <h6 class="modal-title fw-bold"><i class="fal fa-cash-register me-2"></i>Abrir Caja</h6>
            <button type="button" class="btn-close" @click="showOpenCashModal = false"></button>
          </div>
          <div class="modal-body">
            <label class="form-label small text-muted">Fondo inicial (Bs.)</label>
            <input type="number" class="form-control" v-model.number="openingAmount" min="0" step="0.01" placeholder="0.00" />
            <small class="text-muted">Ingresa el monto de efectivo con que inicias el turno.</small>
          </div>
          <div class="modal-footer py-2">
            <button class="btn btn-outline-secondary btn-sm" @click="showOpenCashModal = false">Cancelar</button>
            <button class="btn btn-success btn-sm" :disabled="savingCash" @click="doOpenCash">
              <span v-if="savingCash" class="spinner-border spinner-border-sm me-1"></span>
              <i v-else class="fal fa-unlock me-1"></i>Abrir
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ══ MODAL: Cerrar Caja ══ -->
    <div v-if="showCloseCashModal && cashSession" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header py-2">
            <h6 class="modal-title fw-bold"><i class="fal fa-lock me-2"></i>Cerrar Caja</h6>
            <button type="button" class="btn-close" @click="showCloseCashModal = false"></button>
          </div>
          <div class="modal-body">
            <!-- Resumen del turno -->
            <div class="row g-2 mb-3">
              <div class="col-6">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Fondo inicial</small>
                  <strong>Bs. {{ formatNum(cashSession.OpeningAmount) }}</strong>
                </div>
              </div>
              <div class="col-6">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Ventas</small>
                  <strong class="text-success">Bs. {{ formatNum(cashSession.TotalSales) }}</strong>
                </div>
              </div>
              <div class="col-6" v-if="cashSession.TotalExpenses > 0">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Gastos</small>
                  <strong class="text-danger">− Bs. {{ formatNum(cashSession.TotalExpenses) }}</strong>
                </div>
              </div>
              <div class="col-6" v-if="cashSession.TotalWithdrawals > 0">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Retiros</small>
                  <strong class="text-danger">− Bs. {{ formatNum(cashSession.TotalWithdrawals) }}</strong>
                </div>
              </div>
            </div>
            <div class="alert alert-info py-2 small mb-3">
              <strong>Esperado en caja:</strong> Bs. {{ formatNum(expectedCash) }}
            </div>
            <div class="mb-2">
              <label class="form-label small text-muted">Monto físico contado (Bs.)</label>
              <input type="number" class="form-control" v-model.number="declaredAmount" min="0" step="0.01" placeholder="0.00" />
            </div>
            <div v-if="declaredAmount !== null" class="d-flex justify-content-between small">
              <span class="text-muted">Diferencia</span>
              <span :class="(declaredAmount - expectedCash) >= 0 ? 'text-success fw-semibold' : 'text-danger fw-semibold'">
                Bs. {{ formatNum(declaredAmount - expectedCash) }}
              </span>
            </div>
            <div class="mb-2 mt-2">
              <label class="form-label small text-muted">Observaciones (opcional)</label>
              <textarea class="form-control form-control-sm" rows="2" v-model="closeNotes" placeholder="Ej: faltante por billete roto..."></textarea>
            </div>
          </div>
          <div class="modal-footer py-2">
            <button class="btn btn-outline-secondary btn-sm" @click="showCloseCashModal = false">Cancelar</button>
            <button class="btn btn-danger btn-sm" :disabled="savingCash" @click="doCloseCash">
              <span v-if="savingCash" class="spinner-border spinner-border-sm me-1"></span>
              <i v-else class="fal fa-lock me-1"></i>Cerrar y arquear
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ══ MODAL: Registrar Movimiento (Gasto / Retiro / Ingreso) ══ -->
    <div v-if="showMovementModal && cashSession" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered modal-sm">
        <div class="modal-content">
          <div class="modal-header py-2">
            <h6 class="modal-title fw-bold"><i class="fal fa-receipt me-2"></i>Registrar Movimiento</h6>
            <button type="button" class="btn-close" @click="showMovementModal = false"></button>
          </div>
          <div class="modal-body">
            <div class="mb-2">
              <label class="form-label small text-muted">Tipo</label>
              <div class="d-flex gap-2">
                <button v-for="t in movementTypes" :key="t.value" type="button"
                  class="btn btn-sm flex-fill"
                  :class="movementType === t.value ? t.activeClass : 'btn-outline-secondary'"
                  @click="movementType = t.value">
                  <i :class="t.icon" class="me-1"></i>{{ t.label }}
                </button>
              </div>
            </div>
            <div class="mb-2">
              <label class="form-label small text-muted">Monto (Bs.)</label>
              <input type="number" class="form-control form-control-sm" v-model.number="movementAmount" min="0.01" step="0.01" placeholder="0.00" />
            </div>
            <div class="mb-2">
              <label class="form-label small text-muted">Descripción <span class="text-danger">*</span></label>
              <input type="text" class="form-control form-control-sm" v-model="movementDescription" placeholder="Ej: Almuerzo cajero" maxlength="255" />
            </div>
          </div>
          <div class="modal-footer py-2">
            <button class="btn btn-outline-secondary btn-sm" @click="showMovementModal = false">Cancelar</button>
            <button class="btn btn-primary btn-sm" :disabled="savingMovement" @click="doAddMovement">
              <span v-if="savingMovement" class="spinner-border spinner-border-sm me-1"></span>
              <i v-else class="fal fa-save me-1"></i>Guardar
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ══ BLOQUEO: sin caja abierta ══ -->
    <div v-if="!cashSession" class="d-flex flex-column align-items-center justify-content-center py-5 text-center">
      <i class="fal fa-cash-register fa-4x text-muted mb-3"></i>
      <h5 class="fw-semibold mb-1">Caja cerrada</h5>
      <p class="text-muted mb-3">Debes abrir la caja antes de realizar ventas.</p>
      <button class="btn btn-success px-4" @click="showOpenCashModal = true">
        <i class="fal fa-unlock me-2"></i>Abrir caja
      </button>
    </div>

    <!-- ══ MÓVIL: cliente + tabs ══ -->
    <template v-else>
    <div class="d-md-none mb-2" style="position:relative">
      <!-- Selector de cliente -->
      <div class="card mb-2">
        <div class="card-body py-2 px-3">
          <div v-if="!selectedCustomer">
            <div class="input-group input-group-sm">
              <span class="input-group-text"><i class="fal fa-user"></i></span>
              <input
                type="text" class="form-control" placeholder="Buscar cliente..."
                v-model="customerSearch" @keyup.enter="searchCustomers"
              />
              <button class="btn btn-outline-secondary" type="button" @click="searchCustomers">
                <i class="fal fa-search"></i>
              </button>
            </div>
            <div
              v-if="customerResults.length > 0"
              class="list-group shadow position-absolute start-0 end-0 mx-3"
              style="z-index:1060; top:100%"
            >
              <button
                v-for="c in customerResults" :key="c.Id" type="button"
                class="list-group-item list-group-item-action py-2 px-3"
                @click="selectCustomer(c)"
              >
                <div class="fw-semibold">{{ c.FullName }}</div>
                <small class="text-muted">{{ c.DocumentNumber }}</small>
              </button>
            </div>
          </div>
          <div v-else class="d-flex align-items-center justify-content-between">
            <div>
              <i class="fal fa-user text-primary me-1"></i>
              <span class="fw-semibold">{{ selectedCustomer.FullName }}</span>
            </div>
            <div class="d-flex align-items-center gap-2">
              <span class="fw-bold text-primary small">Bs. {{ formatNum(total) }}</span>
              <button type="button" class="btn btn-outline-secondary btn-sm py-0 px-1" @click="clearCustomer">
                <i class="fal fa-times"></i>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Tabs productos / carrito -->
      <ul class="nav nav-pills nav-fill mb-2">
        <li class="nav-item">
          <button
            class="nav-link w-100"
            :class="activeTab === 'products' ? 'active' : ''"
            @click="activeTab = 'products'"
          >
            <i class="fal fa-th me-1"></i>Productos
          </button>
        </li>
        <li class="nav-item">
          <button
            class="nav-link w-100 position-relative"
            :class="activeTab === 'cart' ? 'active' : ''"
            @click="activeTab = 'cart'"
          >
            <i class="fal fa-shopping-cart me-1"></i>Carrito
            <span
              v-if="totalItems > 0"
              class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
              style="font-size:0.65rem"
            >{{ totalItems }}</span>
          </button>
        </li>
      </ul>
    </div>

    <!-- ══ LAYOUT SPLIT ══ -->
    <div class="pos-split">

      <!-- ════ PANEL IZQUIERDO: carrito ════ -->
      <div
        class="pos-cart-panel"
        :class="activeTab === 'cart' ? '' : 'd-none d-md-flex'"
      >
        <!-- Zona superior: cliente + lista (crece y hace scroll en desktop) -->
        <div class="pos-cart-body">

        <!-- Cliente (solo desktop) -->
        <div class="card mb-2 d-none d-md-block" style="position:relative">
          <div class="card-header py-2 px-3">
            <span class="fw-semibold small"><i class="fal fa-user me-1 text-primary"></i>Cliente</span>
          </div>
          <div class="card-body py-2 px-3">
            <div v-if="!selectedCustomer">
              <div class="input-group input-group-sm">
                <input
                  type="text" class="form-control" placeholder="Buscar cliente..."
                  v-model="customerSearch" @keyup.enter="searchCustomers"
                />
                <button class="btn btn-outline-secondary" type="button" @click="searchCustomers">
                  <i class="fal fa-search"></i>
                </button>
              </div>
              <div
                v-if="customerResults.length > 0"
                class="list-group shadow position-absolute start-0 end-0 mx-2"
                style="z-index:1060; top:100%"
              >
                <button
                  v-for="c in customerResults" :key="c.Id" type="button"
                  class="list-group-item list-group-item-action py-2 px-3"
                  @click="selectCustomer(c)"
                >
                  <div class="fw-semibold">{{ c.FullName }}</div>
                  <small class="text-muted">{{ c.DocumentNumber }}</small>
                </button>
              </div>
              <small class="text-muted d-block mt-1">Ingresa nombre y presiona Enter</small>
            </div>
            <div v-else class="d-flex align-items-center justify-content-between">
              <div>
                <div class="fw-semibold">{{ selectedCustomer.FullName }}</div>
                <small class="text-muted">{{ selectedCustomer.DocumentNumber }}</small>
              </div>
              <button type="button" class="btn btn-outline-secondary btn-sm" @click="clearCustomer">
                <i class="fal fa-times"></i>
              </button>
            </div>
          </div>
        </div>

        <!-- Items del carrito -->
        <div class="card mb-2 pos-cart-items-card">
          <div class="card-header py-2 px-3">
            <span class="fw-semibold small">
              <i class="fal fa-shopping-cart me-1 text-primary"></i>Carrito
              <span v-if="totalItems > 0" class="badge bg-primary ms-1">{{ totalItems }}</span>
            </span>
          </div>

          <div v-if="cart.length === 0" class="card-body text-center py-4">
            <i class="fal fa-cart-plus fa-2x text-muted d-block mb-2"></i>
            <small class="text-muted">Sin productos.<br>Selecciona del catálogo.</small>
          </div>

          <div v-else class="list-group list-group-flush pos-cart-list">
            <div
              class="list-group-item px-3 py-2"
              v-for="(line, i) in cart" :key="i"
            >
              <div class="d-flex justify-content-between align-items-start mb-1">
                <span class="fw-semibold lh-sm" style="font-size:0.85rem">{{ line.ProductName }}</span>
                <div class="d-flex align-items-center gap-1 ms-1">
                  <button
                    type="button"
                    class="btn btn-sm py-0 px-1 lh-1"
                    :class="line.DiscountLabel ? 'btn-success' : 'btn-outline-secondary'"
                    @click="openDiscountModal(i)"
                    title="Aplicar descuento"
                  ><i class="fal fa-percent" style="font-size:0.68rem"></i></button>
                  <button
                    type="button" class="btn btn-link btn-sm text-danger p-0"
                    @click="removeFromCart(i)"
                  ><i class="fal fa-times-circle"></i></button>
                </div>
              </div>
              <!-- Descuento aplicado en línea -->
              <div v-if="line.DiscountLabel" class="d-flex justify-content-between align-items-center mb-1">
                <div class="d-flex align-items-center gap-1">
                  <small class="text-success" style="font-size:0.7rem">
                    <i class="fal fa-tag me-1"></i>{{ line.DiscountLabel }}
                  </small>
                  <button type="button" class="btn btn-link p-0 text-danger lh-1" @click="removeLineDiscount(i)" title="Quitar descuento">
                    <i class="fal fa-times" style="font-size:0.65rem"></i>
                  </button>
                </div>
                <small class="text-success fw-semibold" style="font-size:0.7rem">− Bs. {{ formatNum(line.LineTotalDiscounts) }}</small>
              </div>
              <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center gap-1">
                  <button
                    type="button"
                    class="btn btn-outline-secondary btn-sm py-0 px-2 lh-1"
                    @click="decreaseQty(i)"
                  >−</button>
                  <span class="fw-bold px-2">{{ line.Quantity }}</span>
                  <button
                    type="button"
                    class="btn btn-outline-secondary btn-sm py-0 px-2 lh-1"
                    @click="increaseQty(i)"
                  >+</button>
                </div>
                <div class="text-end">
                  <small class="text-muted d-block" style="font-size:0.72rem">
                    Bs. {{ formatNum(line.UnitPrice) }} × {{ line.Quantity }}
                  </small>
                  <span class="fw-bold text-primary" style="font-size:0.9rem">
                    Bs. {{ formatNum(line.LineTotal) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        </div><!-- /pos-cart-body -->

        <!-- Zona inferior: totales + botones (pegada al fondo en desktop) -->
        <div class="pos-cart-footer">

        <!-- Totales -->
        <div class="card mb-2" v-if="cart.length > 0">
          <div class="card-body py-2 px-3">
            <div class="d-flex justify-content-between mb-1">
              <small class="text-muted">Subtotal</small>
              <small>Bs. {{ formatNum(subtotal) }}</small>
            </div>
            <div class="d-flex justify-content-between mb-1" v-if="totalLineDiscounts > 0">
              <small class="text-muted">Desc. por línea</small>
              <small class="text-success fw-semibold">− Bs. {{ formatNum(totalLineDiscounts) }}</small>
            </div>
            <!-- Descuento global aplicado -->
            <div v-if="headerDiscountAmount > 0" class="d-flex justify-content-between align-items-center mb-1">
              <div class="d-flex align-items-center gap-1">
                <small class="text-muted">{{ headerDiscountLabel }}</small>
                <button type="button" class="btn btn-link p-0 text-danger lh-1" @click="removeHeaderDiscount" title="Quitar descuento global">
                  <i class="fal fa-times" style="font-size:0.65rem"></i>
                </button>
              </div>
              <small class="text-success fw-semibold">− Bs. {{ formatNum(headerDiscountAmount) }}</small>
            </div>
            <!-- Botón agregar descuento global -->
            <div class="mb-1">
              <button
                type="button"
                class="btn btn-sm w-100 py-0"
                :class="headerDiscountAmount > 0 ? 'btn-outline-success' : 'btn-outline-secondary'"
                @click="openDiscountModal(-1)"
              >
                <i class="fal fa-tag me-1"></i>
                {{ headerDiscountAmount > 0 ? 'Cambiar descuento global' : 'Agregar descuento global' }}
              </button>
            </div>
            <div class="d-flex justify-content-between align-items-center border-top pt-2 mt-1">
              <span class="fw-bold">TOTAL</span>
              <span class="fw-bold fs-5 text-primary">Bs. {{ formatNum(total) }}</span>
            </div>
          </div>
        </div>

        <!-- Botones de acción -->
        <div class="d-grid gap-2">
          <button
            type="button"
            class="btn btn-primary"
            :disabled="cart.length === 0 || !selectedCustomer"
            @click="confirmSale"
          >
            <i class="fal fa-check-circle me-1"></i>
            {{ selectedCustomer ? 'Cobrar — Bs. ' + formatNum(total) : 'Selecciona un cliente' }}
          </button>
          <button
            type="button"
            class="btn btn-outline-secondary btn-sm"
            :disabled="cart.length === 0 && !selectedCustomer"
            @click="resetAll"
          >
            <i class="fal fa-trash-alt me-1"></i>Limpiar todo
          </button>
        </div>

        </div><!-- /pos-cart-footer -->
      </div><!-- /pos-cart-panel -->

      <!-- ════ MODAL: Aplicar Descuento ════ -->
      <div v-if="showDiscountModal" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
        <div class="modal-dialog modal-dialog-centered modal-sm">
          <div class="modal-content">

            <div class="modal-header py-2">
              <h6 class="modal-title fw-bold">
                <i class="fal fa-tag me-2"></i>
                {{ discountTargetIndex >= 0 ? 'Descuento por línea' : 'Descuento global' }}
              </h6>
              <button type="button" class="btn-close" @click="showDiscountModal = false"></button>
            </div>

            <div class="modal-body">

              <!-- Base amount info -->
              <div class="d-flex justify-content-between small text-muted mb-3">
                <span>Base</span>
                <span class="fw-semibold">Bs. {{ formatNum(discountBaseAmount) }}</span>
              </div>

              <!-- Tabs: Predefinido / Manual -->
              <div class="d-flex gap-0 mb-3 border-bottom">
                <button
                  class="btn btn-sm px-3 pb-2 me-1"
                  style="border-radius:0; border-bottom: 2px solid transparent"
                  :style="discountMode === 'catalog' ? 'border-bottom-color: var(--bs-primary)' : ''"
                  :class="discountMode === 'catalog' ? 'text-primary fw-semibold' : 'text-muted'"
                  @click="discountMode = 'catalog'"
                >
                  <i class="fal fa-list me-1"></i>Predefinido
                </button>
                <button
                  class="btn btn-sm px-3 pb-2"
                  style="border-radius:0; border-bottom: 2px solid transparent"
                  :style="discountMode === 'manual' ? 'border-bottom-color: var(--bs-primary)' : ''"
                  :class="discountMode === 'manual' ? 'text-primary fw-semibold' : 'text-muted'"
                  @click="discountMode = 'manual'"
                >
                  <i class="fal fa-keyboard me-1"></i>Manual
                </button>
              </div>

              <!-- Modo catálogo -->
              <div v-if="discountMode === 'catalog'">
                <div v-if="discountCatalog.length === 0" class="text-center text-muted py-3">
                  <i class="fal fa-tags fa-2x d-block mb-2 opacity-50"></i>
                  <small>Sin descuentos configurados.</small>
                </div>
                <div v-else class="list-group list-group-flush" style="max-height:240px; overflow-y:auto">
                  <button
                    v-for="d in discountCatalog" :key="d.Id"
                    type="button"
                    class="list-group-item list-group-item-action py-2 px-3 d-flex justify-content-between align-items-center"
                    :class="selectedDiscountId === d.Id ? 'active' : ''"
                    @click="selectedDiscountId = d.Id"
                  >
                    <div>
                      <div class="fw-semibold small">{{ d.Name }}</div>
                      <small class="opacity-75" v-if="d.Description">{{ d.Description }}</small>
                    </div>
                    <span class="badge ms-2 flex-shrink-0"
                      :class="selectedDiscountId === d.Id ? 'bg-white text-primary' : 'bg-primary-subtle text-primary'">
                      {{ d.Type === 'Percentage' ? d.Value + '%' : 'Bs. ' + formatNum(d.Value) }}
                    </span>
                  </button>
                </div>
              </div>

              <!-- Modo manual -->
              <div v-if="discountMode === 'manual'">
                <div class="mb-3">
                  <label class="form-label small text-muted mb-1">Tipo de descuento</label>
                  <div class="d-flex gap-2">
                    <button type="button" class="btn btn-sm flex-fill"
                      :class="manualDiscountType === 'Percentage' ? 'btn-primary' : 'btn-outline-secondary'"
                      @click="manualDiscountType = 'Percentage'">
                      <i class="fal fa-percent me-1"></i>Porcentaje
                    </button>
                    <button type="button" class="btn btn-sm flex-fill"
                      :class="manualDiscountType === 'FixedAmount' ? 'btn-primary' : 'btn-outline-secondary'"
                      @click="manualDiscountType = 'FixedAmount'">
                      <i class="fal fa-dollar-sign me-1"></i>Monto fijo
                    </button>
                  </div>
                </div>
                <div class="mb-2">
                  <label class="form-label small text-muted mb-1">
                    {{ manualDiscountType === 'Percentage' ? 'Porcentaje (%)' : 'Monto (Bs.)' }}
                  </label>
                  <input
                    type="number" class="form-control form-control-sm"
                    v-model.number="manualDiscountValue"
                    :max="manualDiscountType === 'Percentage' ? 100 : undefined"
                    min="0.01" step="0.01" placeholder="0"
                  />
                </div>
              </div>

              <!-- Preview del descuento -->
              <div v-if="discountPreview > 0" class="alert alert-success py-2 mb-0 mt-2 small">
                <i class="fal fa-check-circle me-1"></i>
                <strong>Ahorro:</strong> Bs. {{ formatNum(discountPreview) }}
              </div>

            </div>

            <div class="modal-footer py-2">
              <button class="btn btn-outline-secondary btn-sm" @click="showDiscountModal = false">Cancelar</button>
              <button class="btn btn-success btn-sm" :disabled="!canApplyDiscount" @click="applyDiscount">
                <i class="fal fa-check me-1"></i>Aplicar
              </button>
            </div>

          </div>
        </div>
      </div><!-- /modal descuento -->

      <!-- ════ MODAL: Autorización Supervisor (Fase 3) ════ -->
      <div v-if="showSupervisorModal" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.65)">
        <div class="modal-dialog modal-dialog-centered modal-sm">
          <div class="modal-content">

            <div class="modal-header py-2 bg-warning-subtle">
              <h6 class="modal-title fw-bold text-warning-emphasis">
                <i class="fal fa-shield-alt me-2"></i>Autorización requerida
              </h6>
              <button type="button" class="btn-close" @click="showSupervisorModal = false"></button>
            </div>

            <div class="modal-body">
              <p class="small text-muted mb-3">
                El descuento manual supera el límite permitido para cajeros
                (<strong>{{ maxCashierDiscountPct }}%</strong> por porcentaje /
                <strong>Bs. {{ maxCashierDiscountAmount }}</strong> por monto fijo).
                Ingresa las credenciales de un supervisor para autorizar.
              </p>

              <div class="mb-2">
                <label class="form-label small text-muted">Email del supervisor</label>
                <input
                  type="email" class="form-control form-control-sm"
                  v-model="supervisorEmail"
                  placeholder="supervisor@empresa.com"
                  autocomplete="off"
                />
              </div>
              <div class="mb-2">
                <label class="form-label small text-muted">Contraseña</label>
                <input
                  type="password" class="form-control form-control-sm"
                  v-model="supervisorPassword"
                  placeholder="••••••••"
                  @keyup.enter="verifySupervisor"
                />
              </div>

              <div v-if="supervisorError" class="alert alert-danger py-2 small mb-0 mt-2">
                <i class="fal fa-exclamation-triangle me-1"></i>{{ supervisorError }}
              </div>
            </div>

            <div class="modal-footer py-2">
              <button type="button" class="btn btn-outline-secondary btn-sm" @click="showSupervisorModal = false">
                Cancelar
              </button>
              <button type="button" class="btn btn-warning btn-sm" :disabled="supervisorLoading" @click="verifySupervisor">
                <span v-if="supervisorLoading" class="spinner-border spinner-border-sm me-1"></span>
                <i v-else class="fal fa-unlock me-1"></i>Autorizar
              </button>
            </div>

          </div>
        </div>
      </div><!-- /modal supervisor -->

      <!-- ════ MODAL DE COBRO ════ -->
      <div v-if="showPaymentModal" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">

            <div class="modal-header py-2">
              <h6 class="modal-title fw-bold">
                <i class="fal fa-cash-register me-2"></i>Cobrar venta
              </h6>
              <button type="button" class="btn-close" @click="closePaymentModal"></button>
            </div>

            <div class="modal-body">

              <!-- Total a cobrar -->
              <div class="d-flex justify-content-between align-items-center mb-3 p-2 rounded bg-primary bg-opacity-10 border border-primary border-opacity-25">
                <span class="fw-semibold text-primary">Total a cobrar</span>
                <span class="fw-bold fs-5 text-primary">Bs. {{ formatNum(total) }}</span>
              </div>

              <!-- Selector de método + monto -->
              <div class="mb-2">
                <small class="text-muted fw-semibold d-block mb-2">Seleccionar método de pago</small>
                <div class="d-flex flex-wrap gap-2 mb-3">
                  <button
                    v-for="m in paymentMethods"
                    :key="m.Id"
                    type="button"
                    class="btn btn-sm"
                    :class="selectedMethodId === m.Id ? 'btn-primary' : 'btn-outline-secondary'"
                    @click="selectMethod(m)"
                  >
                    <i :class="m.IconCss" class="me-1"></i>{{ m.Name }}
                  </button>
                </div>

                <div class="input-group input-group-sm" v-if="selectedMethodId">
                  <span class="input-group-text">Bs.</span>
                  <input
                    type="number"
                    class="form-control"
                    placeholder="Monto"
                    v-model.number="currentAmount"
                    min="0"
                    step="0.01"
                    @keyup.enter="addPaymentLine"
                  />
                  <button class="btn btn-success" type="button" @click="addPaymentLine" :disabled="currentAmount <= 0">
                    <i class="fal fa-plus me-1"></i>Agregar
                  </button>
                </div>
              </div>

              <!-- Líneas de pago agregadas -->
              <div v-if="paymentLines.length > 0" class="mb-3">
                <small class="text-muted fw-semibold d-block mb-1">Pagos registrados</small>
                <div
                  v-for="(line, i) in paymentLines"
                  :key="i"
                  class="d-flex align-items-center justify-content-between py-1 px-2 mb-1 rounded border"
                >
                  <div>
                    <i :class="line.IconCss" class="me-1 text-muted"></i>
                    <span class="small">{{ line.PaymentMethodName }}</span>
                  </div>
                  <div class="d-flex align-items-center gap-2">
                    <span class="fw-semibold small">Bs. {{ formatNum(line.AmountGiven) }}</span>
                    <button type="button" class="btn btn-sm btn-outline-danger py-0 px-1" @click="removePaymentLine(i)">
                      <i class="fal fa-times"></i>
                    </button>
                  </div>
                </div>
              </div>

              <!-- Resumen de totales -->
              <div class="border-top pt-2">
                <div class="d-flex justify-content-between small mb-1">
                  <span class="text-muted">Total pagado</span>
                  <span :class="totalPaid >= total ? 'text-success fw-semibold' : 'text-danger fw-semibold'">
                    Bs. {{ formatNum(totalPaid) }}
                  </span>
                </div>
                <div class="d-flex justify-content-between small mb-1" v-if="totalPaid < total">
                  <span class="text-muted">Pendiente</span>
                  <span class="text-danger fw-semibold">Bs. {{ formatNum(total - totalPaid) }}</span>
                </div>
                <div class="d-flex justify-content-between small" v-if="totalChange > 0">
                  <span class="text-muted">Vuelto</span>
                  <span class="text-success fw-semibold">Bs. {{ formatNum(totalChange) }}</span>
                </div>
              </div>

            </div><!-- /modal-body -->

            <div class="modal-footer py-2">
              <button type="button" class="btn btn-outline-secondary btn-sm" @click="closePaymentModal">
                Cancelar
              </button>
              <button
                type="button"
                class="btn btn-primary btn-sm"
                :disabled="totalPaid < total || savingPayment"
                @click="finalizeSale"
              >
                <span v-if="savingPayment" class="spinner-border spinner-border-sm me-1"></span>
                <i v-else class="fal fa-check me-1"></i>
                Confirmar venta
              </button>
            </div>

          </div>
        </div>
      </div><!-- /modal cobro -->

      <!-- ════ MODAL VENTA COMPLETADA ════ -->
      <Teleport to="body">
        <Transition name="fade-modal">
          <div v-if="showCompletedModal" class="completed-overlay">
            <div class="completed-card shadow-lg">
              <!-- Icono éxito -->
              <div class="text-center mb-3">
                <div class="completed-check-icon">
                  <i class="fal fa-check-circle"></i>
                </div>
                <h4 class="fw-700 mb-0 mt-2">Venta Completada</h4>
                <small class="text-muted">{{ completedDate }}</small>
              </div>

              <!-- Cliente -->
              <div class="text-center mb-3">
                <span class="badge bg-light text-dark border px-3 py-2">
                  <i class="fal fa-user me-1"></i>{{ completedCustomer }}
                </span>
              </div>

              <!-- Total y vuelto -->
              <div class="row g-2 mb-3">
                <div class="col-6">
                  <div class="card bg-primary text-white text-center py-3">
                    <div class="small opacity-75">Total cobrado</div>
                    <div class="fw-bold fs-5">Bs. {{ formatNum2(completedTotal) }}</div>
                  </div>
                </div>
                <div class="col-6">
                  <div class="card bg-success text-white text-center py-3">
                    <div class="small opacity-75">Vuelto</div>
                    <div class="fw-bold fs-5">Bs. {{ formatNum2(completedChange) }}</div>
                  </div>
                </div>
              </div>

              <!-- Métodos de pago -->
              <div class="d-flex flex-wrap gap-1 justify-content-center mb-4">
                <span v-for="(p, i) in completedPayments" :key="i"
                  class="badge bg-secondary px-2 py-1">
                  <i :class="p.IconCss" class="me-1"></i>{{ p.PaymentMethodName }}
                  Bs. {{ formatNum2(p.AmountGiven) }}
                </span>
              </div>

              <!-- Botones -->
              <div class="d-grid gap-2">
                <button class="btn btn-primary btn-lg" @click="newOrder">
                  <i class="fal fa-plus me-2"></i>Nueva Orden
                </button>
                <div class="d-flex gap-2">
                  <button class="btn btn-outline-secondary flex-fill" @click="printReceipt">
                    <i class="fal fa-print me-1"></i>Imprimir recibo
                  </button>
                  <button class="btn btn-outline-warning flex-fill" @click="goToReturn">
                    <i class="fal fa-undo me-1"></i>Devolver venta
                  </button>
                </div>
              </div>
            </div>

            <!-- Recibo oculto para impresión -->
            <div class="receipt-print">
              <div style="text-align:center; margin-bottom:12px;">
                <strong style="font-size:1.2em;">RECIBO DE VENTA</strong><br>
                <span>{{ completedDate }}</span>
              </div>
              <div>Cliente: {{ completedCustomer }}</div>
              <hr>
              <div v-for="(d, i) in completedDetail" :key="i">
                <div style="display:flex; justify-content:space-between;">
                  <span>{{ d.ProductName }} x{{ d.Quantity }}</span>
                  <span>Bs. {{ formatNum2(d.LineSubtotal) }}</span>
                </div>
                <div v-if="d.LineTotalDiscounts > 0" style="display:flex; justify-content:space-between; font-size:0.85em; color:#555">
                  <span style="padding-left:8px">{{ d.DiscountLabel || 'Descuento' }}</span>
                  <span>− Bs. {{ formatNum2(d.LineTotalDiscounts) }}</span>
                </div>
              </div>
              <hr>
              <div v-if="completedTotalLineDiscounts > 0" style="display:flex; justify-content:space-between;">
                <span>Desc. por línea</span>
                <span>− Bs. {{ formatNum2(completedTotalLineDiscounts) }}</span>
              </div>
              <div v-if="completedHeaderDiscountAmount > 0" style="display:flex; justify-content:space-between;">
                <span>Desc. global</span>
                <span>− Bs. {{ formatNum2(completedHeaderDiscountAmount) }}</span>
              </div>
              <div style="display:flex; justify-content:space-between; font-weight:bold; margin-top:4px">
                <span>TOTAL</span>
                <span>Bs. {{ formatNum2(completedTotal) }}</span>
              </div>
              <div style="display:flex; justify-content:space-between;">
                <span>Vuelto</span>
                <span>Bs. {{ formatNum2(completedChange) }}</span>
              </div>
            </div>
          </div>
        </Transition>
      </Teleport>

      <!-- ════ PANEL DERECHO: catálogo ════ -->
      <div
        class="pos-catalog-panel"
        :class="activeTab === 'products' ? '' : 'd-none d-md-flex'"
      >
        <!-- Buscador + pills (sticky en desktop) -->
        <div class="pos-catalog-head">
          <!-- Buscador de productos -->
          <div class="card mb-2">
            <div class="card-body py-2 px-3">
              <div class="input-group input-group-sm">
                <span class="input-group-text"><i class="fal fa-search"></i></span>
                <input
                  type="text" class="form-control"
                  placeholder="Buscar producto por nombre..."
                  v-model="productSearch"
                  ref="productInputRef"
                />
                <button
                  v-if="productSearch"
                  class="btn btn-outline-secondary" type="button"
                  @click="productSearch = ''"
                >
                  <i class="fal fa-times"></i>
                </button>
              </div>
            </div>
          </div>

          <!-- Pills de categoría -->
          <div class="mb-2 d-flex flex-wrap gap-1" v-if="categoryFilters.length > 0">
            <button
              type="button" class="btn btn-sm"
              :class="selectedCategory === '' ? 'btn-primary' : 'btn-outline-secondary'"
              @click="selectedCategory = ''"
            >Todos</button>
            <button
              v-for="cat in categoryFilters" :key="cat"
              type="button" class="btn btn-sm"
              :class="selectedCategory === cat ? 'btn-primary' : 'btn-outline-secondary'"
              @click="selectedCategory = cat === selectedCategory ? '' : cat"
            >{{ cat }}</button>
          </div>
        </div>

        <!-- Grid scrollable (solo esta sección hace scroll) -->
        <div class="pos-catalog-grid">

        <!-- Cargando -->
        <div v-if="loadingProducts" class="text-center py-5">
          <i class="fal fa-spinner fa-spin fa-2x text-muted"></i>
          <p class="text-muted mt-2 mb-0">Cargando catálogo...</p>
        </div>

        <!-- Sin resultados -->
        <div v-else-if="filteredProducts.length === 0" class="card">
          <div class="card-body text-center py-5">
            <i class="fal fa-search fa-2x text-muted d-block mb-2"></i>
            <p class="text-muted mb-0">No se encontraron productos.</p>
          </div>
        </div>

        <!-- Grid de productos -->
        <div v-else class="row g-2">
          <div
            class="col-6 col-sm-4 col-lg-3"
            v-for="prod in filteredProducts" :key="prod.Id"
          >
            <div
              class="card h-100 product-card"
              :class="{
                'border-primary shadow-sm': cartQty(prod.Id) > 0,
                'product-card--disabled': prod.CurrentStock <= 0
              }"
              @click="prod.CurrentStock > 0 && addToCart(prod)"
            >
              <!-- Badge cantidad en carrito -->
              <span
                v-if="cartQty(prod.Id) > 0"
                class="position-absolute top-0 end-0 translate-middle badge rounded-pill bg-primary"
                style="font-size:0.65rem; z-index:2; margin:0.25rem"
              >{{ cartQty(prod.Id) }}</span>

              <div class="card-body p-2 d-flex flex-column">
                <!-- Nombre -->
                <div
                  class="fw-semibold mb-1 lh-sm"
                  style="font-size:0.82rem; min-height:2.5em; overflow:hidden; display:-webkit-box; -webkit-line-clamp:2; -webkit-box-orient:vertical"
                >{{ prod.ProductName }}</div>

                <!-- Lab -->
                <small class="text-muted mb-2" style="font-size:0.72rem">
                  {{ prod.LaboratoryName || '—' }}
                </small>

                <div class="mt-auto">
                  <!-- Precio + stock -->
                  <div class="d-flex justify-content-between align-items-center mb-2">
                    <span class="fw-bold text-primary" style="font-size:0.9rem">
                      Bs. {{ formatNum(prod.SalePrice) }}
                    </span>
                    <small
                      style="font-size:0.7rem"
                      :class="prod.CurrentStock > 5 ? 'text-success' : prod.CurrentStock > 0 ? 'text-warning' : 'text-danger'"
                    >
                      <i class="fal fa-box me-1"></i>
                      {{ prod.CurrentStock > 0 ? prod.CurrentStock : 'Agotado' }}
                    </small>
                  </div>

                  <!-- Sin stock -->
                  <div
                    v-if="prod.CurrentStock <= 0"
                    class="btn btn-sm btn-outline-secondary w-100 disabled"
                    style="font-size:0.75rem"
                  >Sin stock</div>

                  <!-- No está en carrito -->
                  <button
                    v-else-if="cartQty(prod.Id) === 0"
                    type="button"
                    class="btn btn-sm btn-outline-primary w-100"
                    style="font-size:0.75rem"
                    @click.stop="addToCart(prod)"
                  >
                    <i class="fal fa-plus me-1"></i>Agregar
                  </button>

                  <!-- Ya está en carrito: controles +/- -->
                  <div v-else class="d-flex align-items-center justify-content-between gap-1">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-secondary flex-fill py-0"
                      @click.stop="decreaseQtyByProduct(prod.Id)"
                    >−</button>
                    <span class="fw-bold text-primary px-1">{{ cartQty(prod.Id) }}</span>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-primary flex-fill py-0"
                      @click.stop="addToCart(prod)"
                    >+</button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        </div><!-- /pos-catalog-grid -->
      </div><!-- /pos-catalog-panel -->
    </div><!-- /pos-split -->
    </template><!-- /v-else caja abierta -->
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Sale } from '@/modules/inventory/models/sale.model';
import { SaleDetail } from '@/modules/inventory/models/saleDetail.model';
import { SalePayment, type PaymentMethod } from '@/modules/inventory/models/paymentMethod.model';
import type { Customer } from '@/modules/inventory/models/customer.model';
import type { Product } from '@/modules/inventory/models/product.model';
import type { CashSession } from '@/modules/inventory/models/cashSession.model';
import { CashMovementRequest } from '@/modules/inventory/models/cashMovement.model';
import type { Discount } from '@/modules/inventory/models/discount.model';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import { getApi } from '@/modules/common/composables/api/getApi';
import useSales from '@/modules/inventory/composables/useSales';
import usePosSettings from '@/modules/inventory/composables/usePosSettings';
import useCustomer from '@/modules/inventory/composables/useCustomer';
import useProduct from '@/modules/inventory/composables/useProduct';
import usePaymentMethod from '@/modules/inventory/composables/usePaymentMethod';
import useCashSession from '@/modules/inventory/composables/useCashSession';
import useCashMovement from '@/modules/inventory/composables/useCashMovement';
import useDiscount from '@/modules/inventory/composables/useDiscount';
import utils from '@/utils/msg';

const router = useRouter();
const authStore = useAuthStore();

// Límites cargados desde el backend (appsettings.json → GET /Settings/pos)
const maxCashierDiscountPct    = ref<number>(15);
const maxCashierDiscountAmount = ref<number>(50);

const { saveSaleApi } = useSales();
const { getCustomers } = useCustomer();
const { getProductsByName } = useProduct();
const { getPaymentMethods } = usePaymentMethod();
const { getActiveSession, openSession, closeSession } = useCashSession();
const { addMovement } = useCashMovement();
const { getDiscounts } = useDiscount();
const { getPosSettings } = usePosSettings();

// ── Estado: caja ───────────────────────────────────────────
const cashSession = ref<CashSession | null>(null);
const showOpenCashModal = ref(false);
const showCloseCashModal = ref(false);
const showMovementModal = ref(false);
const openingAmount = ref<number>(0);
const declaredAmount = ref<number>(0);
const closeNotes = ref('');
const savingCash = ref(false);
const movementType = ref<'expense' | 'withdrawal' | 'income'>('expense');
const movementAmount = ref<number>(0);
const movementDescription = ref('');
const savingMovement = ref(false);

const movementTypes = [
  { value: 'expense' as const,    label: 'Gasto',   icon: 'fal fa-receipt',             activeClass: 'btn-danger' },
  { value: 'withdrawal' as const, label: 'Retiro',  icon: 'fal fa-arrow-circle-up',     activeClass: 'btn-warning' },
  { value: 'income' as const,     label: 'Ingreso', icon: 'fal fa-arrow-circle-down',   activeClass: 'btn-success' },
];

const expectedCash = computed(() => {
  if (!cashSession.value) return 0;
  return +(
    cashSession.value.OpeningAmount +
    cashSession.value.TotalSales -
    cashSession.value.TotalExpenses -
    cashSession.value.TotalWithdrawals +
    cashSession.value.TotalIncome
  ).toFixed(2);
});

// ── Estado ─────────────────────────────────────────────────
const cart = ref<SaleDetail[]>([]);
const allProducts = ref<Product[]>([]);
const loadingProducts = ref(false);
const selectedCustomer = ref<Customer | null>(null);
const customerSearch = ref('');
const customerResults = ref<Customer[]>([]);
const productSearch = ref('');
const selectedCategory = ref('');
const activeTab = ref<'products' | 'cart'>('products');
const productInputRef = ref<HTMLInputElement | null>(null);

// ── Modal de cobro ─────────────────────────────────────────
const paymentMethods = ref<PaymentMethod[]>([]);
const showPaymentModal = ref(false);

// ── Descuentos ─────────────────────────────────────────────
const isCashier = computed(() => authStore.getUser?.RolName === 'Cajero');

const discountCatalog = ref<Discount[]>([]);
const showDiscountModal = ref(false);
const discountTargetIndex = ref(-1);          // -1 = cabecera, >= 0 = índice de línea en cart
const discountMode = ref<'catalog' | 'manual'>('catalog');
const manualDiscountType = ref<'Percentage' | 'FixedAmount'>('Percentage');
const manualDiscountValue = ref<number>(0);
const selectedDiscountId = ref('');
const headerDiscountId = ref('');
const headerDiscountLabel = ref('');
const headerDiscountAmount = ref<number>(0);
const headerDiscountType = ref('');
const headerDiscountValue = ref<number>(0);

// Fase 3 — Autorización supervisor
const showSupervisorModal = ref(false);
const supervisorEmail = ref('');
const supervisorPassword = ref('');
const supervisorError = ref('');
const supervisorLoading = ref(false);
type PendingDiscount = {
  targetIndex: number;
  discountId: string; discountLabel: string; discountType: string;
  discountValue: number; discountAmount: number;
};
const pendingDiscount = ref<PendingDiscount | null>(null);
const supervisorAuthToken = ref('');

// ── Modal venta completada ─────────────────────────────────
const showCompletedModal = ref(false);
const completedSaleId = ref('');
const completedCustomer = ref('');
const completedTotal = ref(0);
const completedChange = ref(0);
const completedPayments = ref<SalePayment[]>([]);
const completedDetail = ref<SaleDetail[]>([]);
const completedDate = ref('');
const completedTotalLineDiscounts = ref(0);
const completedHeaderDiscountAmount = ref(0);
const savingPayment = ref(false);
const selectedMethodId = ref('');
const selectedMethod = ref<PaymentMethod | null>(null);
const currentAmount = ref<number>(0);
const paymentLines = ref<SalePayment[]>([]);

const totalPaid = computed(() =>
  +paymentLines.value.reduce((s, l) => s + l.AmountGiven, 0).toFixed(2)
);
const totalChange = computed(() =>
  +(Math.max(0, totalPaid.value - total.value)).toFixed(2)
);

// ── Computed ───────────────────────────────────────────────
const categoryFilters = computed(() => {
  const cats = allProducts.value
    .map((p: Product) => p.CategoryName)
    .filter((c: string) => c && c.trim() !== '');
  return [...new Set(cats)].sort() as string[];
});

const filteredProducts = computed(() => {
  let list = allProducts.value.filter((p: Product) => p.IsActive);
  if (productSearch.value.trim()) {
    const q = productSearch.value.toLowerCase();
    list = list.filter((p: Product) => p.ProductName.toLowerCase().includes(q));
  }
  if (selectedCategory.value) {
    list = list.filter((p: Product) => p.CategoryName === selectedCategory.value);
  }
  return list;
});

const totalItems = computed(() =>
  cart.value.reduce((s: number, l: SaleDetail) => s + l.Quantity, 0)
);
const subtotal = computed(() =>
  +cart.value.reduce((s: number, l: SaleDetail) => s + l.LineSubtotal, 0).toFixed(2)
);
const totalLineDiscounts = computed(() =>
  +cart.value.reduce((s: number, l: SaleDetail) => s + l.LineTotalDiscounts, 0).toFixed(2)
);
const totalDiscounts = computed(() =>
  +(totalLineDiscounts.value + headerDiscountAmount.value).toFixed(2)
);
const total = computed(() =>
  +(subtotal.value - totalLineDiscounts.value - headerDiscountAmount.value).toFixed(2)
);

const discountBaseAmount = computed(() =>
  discountTargetIndex.value >= 0
    ? (cart.value[discountTargetIndex.value]?.LineSubtotal ?? 0)
    : +(subtotal.value - totalLineDiscounts.value).toFixed(2)
);

const discountPreview = computed(() => {
  const base = discountBaseAmount.value;
  if (discountMode.value === 'catalog') {
    const d = discountCatalog.value.find(x => x.Id === selectedDiscountId.value);
    if (!d) return 0;
    return d.Type === 'Percentage'
      ? +(Math.min(base * d.Value / 100, base)).toFixed(2)
      : +(Math.min(d.Value, base)).toFixed(2);
  }
  if (manualDiscountValue.value <= 0) return 0;
  return manualDiscountType.value === 'Percentage'
    ? +(Math.min(base * manualDiscountValue.value / 100, base)).toFixed(2)
    : +(Math.min(manualDiscountValue.value, base)).toFixed(2);
});

const canApplyDiscount = computed(() =>
  discountMode.value === 'catalog' ? !!selectedDiscountId.value : manualDiscountValue.value > 0
);

// ── Helpers ────────────────────────────────────────────────
const formatNum = (val: number) =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const cartQty = (productId: string): number => {
  const line = cart.value.find((l: SaleDetail) => l.ProductId === productId);
  return line ? line.Quantity : 0;
};

const recalcLine = (i: number) => {
  const l = cart.value[i];
  l.LineSubtotal = +(l.Quantity * l.UnitPrice).toFixed(2);
  if (l.DiscountType === 'Percentage' && l.DiscountValue > 0) {
    l.LineTotalDiscounts = +(Math.min(l.LineSubtotal * l.DiscountValue / 100, l.LineSubtotal)).toFixed(2);
  } else if (l.DiscountType === 'FixedAmount' && l.DiscountValue > 0) {
    l.LineTotalDiscounts = +(Math.min(l.DiscountValue, l.LineSubtotal)).toFixed(2);
  } else {
    l.LineTotalDiscounts = 0;
  }
  l.LineTotal = +(l.LineSubtotal - l.LineTotalDiscounts).toFixed(2);
};

// ── Carga inicial ──────────────────────────────────────────
onMounted(async () => {
  loadingProducts.value = true;
  const [{ Data: products }, { Data: methods }, sessionResp, discountsResp, settingsResp] = await Promise.all([
    getProductsByName(''),
    getPaymentMethods(),
    getActiveSession(),
    getDiscounts(),
    getPosSettings(),
  ]);
  allProducts.value = (products ?? []).filter((p: Product) => p.IsActive && p.CurrentStock >= 0);
  paymentMethods.value = methods ?? [];
  cashSession.value = sessionResp.Data ?? null;
  discountCatalog.value = (discountsResp.Data ?? []).filter((d: Discount) => d.IsActive);
  if (settingsResp.Data) {
    maxCashierDiscountPct.value    = settingsResp.Data.MaxCashierDiscountPct;
    maxCashierDiscountAmount.value = settingsResp.Data.MaxCashierDiscountAmount;
  }
  loadingProducts.value = false;
  if (!cashSession.value) showOpenCashModal.value = true;
  productInputRef.value?.focus();
});

// ── Clientes ───────────────────────────────────────────────
const searchCustomers = async () => {
  if (!customerSearch.value.trim()) return;
  const { Data } = await getCustomers(customerSearch.value.trim());
  customerResults.value = Data ?? [];
};

const selectCustomer = (c: Customer) => {
  selectedCustomer.value = c;
  customerResults.value = [];
  customerSearch.value = '';
};

const clearCustomer = () => {
  selectedCustomer.value = null;
  customerSearch.value = '';
  customerResults.value = [];
};

// ── Carrito ────────────────────────────────────────────────
const addToCart = (prod: Product) => {
  const idx = cart.value.findIndex((l: SaleDetail) => l.ProductId === prod.Id);
  if (idx >= 0) {
    cart.value[idx].Quantity++;
    recalcLine(idx);
  } else {
    const line = new SaleDetail();
    line.ProductId = prod.Id;
    line.ProductName = prod.ProductName;
    line.UnitPrice = prod.SalePrice;
    line.Quantity = 1;
    line.LineTotalDiscounts = 0;
    line.LineSubtotal = +prod.SalePrice.toFixed(2);
    line.LineTotal = line.LineSubtotal;
    cart.value.push(line);
  }
};

const increaseQty = (i: number) => {
  cart.value[i].Quantity++;
  recalcLine(i);
};

const decreaseQty = (i: number) => {
  if (cart.value[i].Quantity <= 1) {
    cart.value.splice(i, 1);
  } else {
    cart.value[i].Quantity--;
    recalcLine(i);
  }
};

const decreaseQtyByProduct = (productId: string) => {
  const idx = cart.value.findIndex((l: SaleDetail) => l.ProductId === productId);
  if (idx >= 0) decreaseQty(idx);
};

const removeFromCart = (i: number) => {
  cart.value.splice(i, 1);
};

// ── Modal de cobro ─────────────────────────────────────────
const selectMethod = (m: PaymentMethod) => {
  selectedMethodId.value = m.Id;
  selectedMethod.value = m;
  currentAmount.value = +(Math.max(0, total.value - totalPaid.value)).toFixed(2);
};

const addPaymentLine = () => {
  if (!selectedMethod.value || currentAmount.value <= 0) return;
  const m = selectedMethod.value;
  const returned = m.RequiresChanges
    ? +(Math.max(0, totalPaid.value + currentAmount.value - total.value)).toFixed(2)
    : 0;
  const line = new SalePayment();
  line.PaymentMethodId = m.Id;
  line.PaymentMethodName = m.Name;
  line.IconCss = m.IconCss;
  line.AmountGiven = +currentAmount.value.toFixed(2);
  line.AmountReturned = returned;
  paymentLines.value.push(line);
  selectedMethodId.value = '';
  selectedMethod.value = null;
  currentAmount.value = 0;
};

const removePaymentLine = (i: number) => {
  paymentLines.value.splice(i, 1);
};

const openPaymentModal = () => {
  paymentLines.value = [];
  selectedMethodId.value = '';
  selectedMethod.value = null;
  currentAmount.value = 0;
  showPaymentModal.value = true;
};

const closePaymentModal = () => {
  showPaymentModal.value = false;
};

// ── Gestión de caja ────────────────────────────────────────
const doOpenCash = async () => {
  savingCash.value = true;
  try {
    const resp = await openSession({ OpeningAmount: openingAmount.value });
    if (resp.ok) {
      const sessionResp = await getActiveSession();
      cashSession.value = sessionResp.Data ?? null;
      showOpenCashModal.value = false;
      openingAmount.value = 0;
    } else {
      utils.showMessageModal({ Description: resp.Message?.Description || 'No se pudo abrir la caja.', MessageType: 'warning' });
    }
  } finally {
    savingCash.value = false;
  }
};

const doCloseCash = async () => {
  if (!cashSession.value) return;
  savingCash.value = true;
  try {
    const resp = await closeSession(cashSession.value.Id, { DeclaredAmount: declaredAmount.value, Notes: closeNotes.value });
    if (resp.ok) {
      cashSession.value = null;
      showCloseCashModal.value = false;
      declaredAmount.value = 0;
      closeNotes.value = '';
      utils.showMessageModal({ Description: 'Caja cerrada correctamente.', MessageType: 'info' });
    } else {
      utils.showMessageModal({ Description: resp.Message?.Description || 'No se pudo cerrar la caja.', MessageType: 'warning' });
    }
  } finally {
    savingCash.value = false;
  }
};

const doAddMovement = async () => {
  if (!cashSession.value) return;
  if (!movementDescription.value.trim()) {
    utils.showMessageModal({ Description: 'La descripción es obligatoria.', MessageType: 'warning' });
    return;
  }
  if (movementAmount.value <= 0) {
    utils.showMessageModal({ Description: 'El monto debe ser mayor a cero.', MessageType: 'warning' });
    return;
  }
  savingMovement.value = true;
  try {
    const request: CashMovementRequest = {
      CashSessionId: cashSession.value.Id,
      MovementType: movementType.value,
      Amount: movementAmount.value,
      Description: movementDescription.value.trim(),
    };
    const resp = await addMovement(cashSession.value.Id, request);
    if (resp.ok) {
      // Recargar sesión para actualizar totales
      const sessionResp = await getActiveSession();
      cashSession.value = sessionResp.Data ?? null;
      showMovementModal.value = false;
      movementAmount.value = 0;
      movementDescription.value = '';
      movementType.value = 'expense';
    } else {
      utils.showMessageModal({ Description: resp.Message?.Description || 'No se pudo registrar el movimiento.', MessageType: 'warning' });
    }
  } finally {
    savingMovement.value = false;
  }
};

// ── Descuentos ─────────────────────────────────────────────
const openDiscountModal = (targetIndex: number) => {
  discountTargetIndex.value = targetIndex;
  if (targetIndex >= 0) {
    const line = cart.value[targetIndex];
    if (line.DiscountId) {
      discountMode.value = 'catalog';
      selectedDiscountId.value = line.DiscountId;
    } else if (line.DiscountType) {
      discountMode.value = 'manual';
      manualDiscountType.value = line.DiscountType as 'Percentage' | 'FixedAmount';
      manualDiscountValue.value = line.DiscountValue;
      selectedDiscountId.value = '';
    } else {
      discountMode.value = 'catalog';
      selectedDiscountId.value = '';
      manualDiscountValue.value = 0;
    }
  } else {
    discountMode.value = 'catalog';
    selectedDiscountId.value = headerDiscountId.value;
    manualDiscountValue.value = 0;
  }
  showDiscountModal.value = true;
};

const buildDiscountPayload = (): PendingDiscount | null => {
  const base = discountBaseAmount.value;
  let discountId = '', discountLabel = '', discountType = '', discountValue = 0, discountAmount = 0;

  if (discountMode.value === 'catalog') {
    const d = discountCatalog.value.find(x => x.Id === selectedDiscountId.value);
    if (!d) return null;
    discountId = d.Id;
    discountType = d.Type;
    discountValue = d.Value;
    discountAmount = d.Type === 'Percentage'
      ? +(Math.min(base * d.Value / 100, base)).toFixed(2)
      : +(Math.min(d.Value, base)).toFixed(2);
    discountLabel = `${d.Name} (${d.Type === 'Percentage' ? d.Value + '%' : 'Bs. ' + formatNum(d.Value)})`;
  } else {
    discountType = manualDiscountType.value;
    discountValue = manualDiscountValue.value;
    discountAmount = manualDiscountType.value === 'Percentage'
      ? +(Math.min(base * discountValue / 100, base)).toFixed(2)
      : +(Math.min(discountValue, base)).toFixed(2);
    discountLabel = manualDiscountType.value === 'Percentage'
      ? `Manual ${discountValue}%`
      : `Manual Bs. ${formatNum(discountValue)}`;
  }
  return { targetIndex: discountTargetIndex.value, discountId, discountLabel, discountType, discountValue, discountAmount };
};

const commitDiscount = (payload: PendingDiscount) => {
  if (payload.targetIndex >= 0) {
    const line = cart.value[payload.targetIndex];
    line.DiscountId = payload.discountId;
    line.DiscountLabel = payload.discountLabel;
    line.DiscountType = payload.discountType;
    line.DiscountValue = payload.discountValue;
    line.LineTotalDiscounts = payload.discountAmount;
    recalcLine(payload.targetIndex);
  } else {
    headerDiscountId.value = payload.discountId;
    headerDiscountLabel.value = payload.discountLabel;
    headerDiscountAmount.value = payload.discountAmount;
    headerDiscountType.value = payload.discountType;
    headerDiscountValue.value = payload.discountValue;
  }
};

const requiresSupervisorAuth = (payload: PendingDiscount): boolean => {
  if (!isCashier.value) return false;
  if (discountMode.value === 'catalog') return false;
  if (payload.discountType === 'Percentage')
    return payload.discountValue > maxCashierDiscountPct.value;
  if (payload.discountType === 'FixedAmount')
    return payload.discountValue > maxCashierDiscountAmount.value;
  return false;
};

const applyDiscount = () => {
  const payload = buildDiscountPayload();
  if (!payload) return;

  if (requiresSupervisorAuth(payload)) {
    pendingDiscount.value = payload;
    supervisorEmail.value = '';
    supervisorPassword.value = '';
    supervisorError.value = '';
    showDiscountModal.value = false;
    showSupervisorModal.value = true;
    return;
  }

  commitDiscount(payload);
  showDiscountModal.value = false;
};

const verifySupervisor = async () => {
  if (!supervisorEmail.value || !supervisorPassword.value) {
    supervisorError.value = 'Ingresa email y contraseña del supervisor.';
    return;
  }
  supervisorLoading.value = true;
  supervisorError.value = '';
  try {
    const api = getApi();
    const resp = await api.post('Login', {
      UserName: '',
      Email: supervisorEmail.value,
      Password: supervisorPassword.value,
      Device: '',
      WithEmail: true,
      LoginFrom: 5,
      LoginWith: 1,
    });
    const data = resp.data;
    if (!data?.ok || !data?.Data?.Token) {
      supervisorError.value = 'Credenciales incorrectas.';
      return;
    }
    if (data.Data.RolName === 'Cajero') {
      supervisorError.value = 'El usuario ingresado no tiene permisos de supervisor.';
      return;
    }
    // Guardar token del supervisor para enviarlo al backend al grabar la venta
    supervisorAuthToken.value = data.Data.Token;
    // Autorizado — aplicar descuento pendiente
    if (pendingDiscount.value) commitDiscount(pendingDiscount.value);
    pendingDiscount.value = null;
    showSupervisorModal.value = false;
  } catch {
    supervisorError.value = 'Error al verificar las credenciales. Intenta nuevamente.';
  } finally {
    supervisorLoading.value = false;
  }
};

const removeLineDiscount = (i: number) => {
  const line = cart.value[i];
  line.DiscountId = '';
  line.DiscountLabel = '';
  line.DiscountType = '';
  line.DiscountValue = 0;
  line.LineTotalDiscounts = 0;
  recalcLine(i);
};

const removeHeaderDiscount = () => {
  headerDiscountId.value = '';
  headerDiscountLabel.value = '';
  headerDiscountAmount.value = 0;
  headerDiscountType.value = '';
  headerDiscountValue.value = 0;
};

// ── Abrir modal al cobrar ──────────────────────────────────
const confirmSale = () => {
  if (!cashSession.value) {
    utils.showMessageModal({ Description: 'Debes abrir la caja antes de realizar ventas.', MessageType: 'warning' });
    showOpenCashModal.value = true;
    return;
  }
  if (!selectedCustomer.value) {
    utils.showMessageModal({ Description: 'Selecciona un cliente antes de cobrar.', MessageType: 'warning' });
    return;
  }
  if (cart.value.length === 0) {
    utils.showMessageModal({ Description: 'El carrito está vacío.', MessageType: 'warning' });
    return;
  }
  openPaymentModal();
};

// ── Guardar venta con pagos ────────────────────────────────
const finalizeSale = async () => {
  savingPayment.value = true;
  try {
    const sale = new Sale();
    sale.CustomerId = selectedCustomer.value!.Id;
    sale.SaleDate = new Date().toISOString();
    sale.IsActive = true;
    sale.CashSessionId = cashSession.value?.Id ?? '';
    sale.Subtotal = subtotal.value;
    sale.TotalDiscounts = totalDiscounts.value;
    sale.Total = total.value;
    sale.HeaderDiscountId = headerDiscountId.value;
    sale.HeaderDiscountAmount = headerDiscountAmount.value;
    sale.HeaderDiscountType = headerDiscountType.value;
    sale.HeaderDiscountValue = headerDiscountValue.value;
    sale.SupervisorAuthToken = supervisorAuthToken.value;
    sale.Detail = cart.value.map((l: SaleDetail) => {
      const d = new SaleDetail();
      d.ProductId = l.ProductId;
      d.Quantity = l.Quantity;
      d.UnitPrice = l.UnitPrice;
      d.LineSubtotal = l.LineSubtotal;
      d.LineTotalDiscounts = l.LineTotalDiscounts;
      d.LineTotal = l.LineTotal;
      d.DiscountId = l.DiscountId;
      d.DiscountType = l.DiscountType;
      d.DiscountValue = l.DiscountValue;
      return d;
    });
    sale.Payments = paymentLines.value.map((l) => {
      const p = new SalePayment();
      p.PaymentMethodId = l.PaymentMethodId;
      p.PaymentMethodName = l.PaymentMethodName;
      p.AmountGiven = l.AmountGiven;
      p.AmountReturned = l.AmountReturned;
      return p;
    });

    const { ok: saved, Message, Data: newSaleId } = await saveSaleApi(sale);
    if (saved) {
      // Recargar sesión para que TotalSales refleje la venta recién registrada
      const sessionResp = await getActiveSession();
      cashSession.value = sessionResp.Data ?? null;
      // Capturar datos antes de resetear
      completedSaleId.value = newSaleId ?? '';
      completedCustomer.value = selectedCustomer.value?.FullName ?? '';
      completedTotal.value = total.value;
      completedChange.value = totalChange.value;
      completedPayments.value = [...paymentLines.value];
      completedDetail.value = [...cart.value];
      completedTotalLineDiscounts.value = totalLineDiscounts.value;
      completedHeaderDiscountAmount.value = headerDiscountAmount.value;
      completedDate.value = new Date().toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
      closePaymentModal();
      resetAll();
      showCompletedModal.value = true;
    } else {
      utils.showMessageModal({
        Description: Message?.Description || 'No se pudo registrar la venta.',
        MessageType: 'error',
      });
    }
  } finally {
    savingPayment.value = false;
  }
};

const resetCart = () => {
  cart.value = [];
  activeTab.value = 'products';
  productInputRef.value?.focus();
};

const resetAll = () => {
  resetCart();
  clearCustomer();
  productSearch.value = '';
  selectedCategory.value = '';
  removeHeaderDiscount();
  supervisorAuthToken.value = '';
};

// ── Acciones post-venta ────────────────────────────────────
const newOrder = () => {
  showCompletedModal.value = false;
  getProductsByName('').then(({ Data }) => {
    allProducts.value = (Data ?? []).filter((p: Product) => p.IsActive && p.CurrentStock >= 0);
  });
  productInputRef.value?.focus();
};

const printReceipt = () => {
  window.print();
};

const goToReturn = () => {
  showCompletedModal.value = false;
  router.push({ name: 'sale-detail', params: { id: completedSaleId.value } });
};

const formatNum2 = (val: number) =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
</script>

<style scoped>
/* ── Layout flex de altura fija solo en desktop ── */

/* Siempre ocupa el ancho completo del contenedor padre .app-content (display:flex row) */
.pos-page {
  width: 100%;
  min-width: 0;
}

@media (min-width: 768px) {
  /* Contenedor raíz: ocupa el alto disponible bajo el header de app */
  .pos-page {
    display: flex;
    flex-direction: column;
    height: calc(100vh - 1rem);
    overflow: hidden;
  }
  /* Cabecera POS (volver + título): altura fija, no hace scroll */
  .pos-top-bar { flex-shrink: 0; }

  /* Cuerpo split: ocupa el espacio restante */
  .pos-split {
    flex: 1;
    display: flex;
    gap: 0.5rem;
    overflow: hidden;
    min-height: 0;
  }

  /* Panel izquierdo (carrito): ancho fijo, flex columna con 3 zonas */
  .pos-cart-panel {
    width: 33.333%;
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }
  /* Zona superior: crece y su lista interna hace scroll */
  .pos-cart-body {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }
  /* La card "Carrito" crece para empujar totales al fondo */
  .pos-cart-items-card {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }
  /* Lista de ítems: único elemento que hace scroll dentro del carrito */
  .pos-cart-list {
    flex: 1;
    overflow-y: auto;
    min-height: 0;
  }
  /* Zona inferior: totales + botones, siempre visible abajo */
  .pos-cart-footer { flex-shrink: 0; }

  /* Panel derecho (catálogo): ocupa el resto, flex columna */
  .pos-catalog-panel {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }
  /* Buscador + pills: altura fija arriba, NO hace scroll */
  .pos-catalog-head { flex-shrink: 0; }

  /* Grid de productos: ÚNICO elemento que hace scroll */
  .pos-catalog-grid {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    min-height: 0;
    padding-bottom: 0.5rem;
  }
}

/* ── Modal venta completada ── */
.completed-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  backdrop-filter: blur(4px);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.completed-card {
  background: var(--bs-body-bg);
  color: var(--bs-body-color);
  border-radius: 16px;
  width: 100%;
  max-width: 420px;
  padding: 28px 24px;
  border: 1px solid var(--bs-border-color);
}

.completed-check-icon {
  font-size: 4rem;
  color: #10b981;
  line-height: 1;
}

.fade-modal-enter-active,
.fade-modal-leave-active { transition: opacity 0.25s ease; }
.fade-modal-enter-from,
.fade-modal-leave-to { opacity: 0; }

/* ── Recibo impresión ── */
.receipt-print { display: none; }

@media print {
  :global(#app) { display: none !important; }
  .receipt-print {
    display: block !important;
    position: fixed;
    top: 0; left: 0;
    width: 100%;
    font-family: monospace;
    font-size: 13px;
    padding: 16px;
    background: #fff;
    color: #000;
  }
}

/* ── Cards de producto ── */
.product-card {
  transition: box-shadow 0.15s ease, border-color 0.15s ease;
  cursor: pointer;
  position: relative;
  overflow: visible;
}
.product-card:hover:not(.product-card--disabled) {
  box-shadow: 0 0.25rem 0.75rem rgba(0, 0, 0, 0.12) !important;
}
.product-card--disabled {
  cursor: not-allowed;
  opacity: 0.55;
}
</style>
