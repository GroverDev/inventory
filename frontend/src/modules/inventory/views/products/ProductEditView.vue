<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de productos</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ product.Id !== '0' ? 'Editar Producto' : 'Nuevo Producto' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div id="panel-1" class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ product.Id !== '0' ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Producto</i></span>
              </h2>
              <span
                v-if="product.Id !== '0'"
                class="badge ms-2"
                :class="product.IsActive ? 'bg-success' : 'bg-secondary'"
              >
                {{ product.IsActive ? 'Activo' : 'Inactivo' }}
              </span>
              <span v-if="!canSave" class="badge bg-secondary ms-2">
                <i class="fal fa-lock me-1"></i>Solo lectura
              </span>
            </div>
            <div class="panel-container show">

              <!-- Barra de acciones -->
              <div class="panel-content pt-0">
                <div class="row align-items-center">
                  <div class="col-8 col-md-8">
                    <div v-if="!canSave" class="text-muted small">
                      <i class="fal fa-eye me-1"></i>
                      Está viendo el producto en modo consulta. No tiene permiso para modificarlo.
                    </div>
                    <div v-if="canSave" class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu dropdown-menu-lg-right">
                          <button v-if="canSave" type="button" class="dropdown-item border-bottom border-1"
                            :disabled="isReadOnly" @click="saveProduct">
                            <span class="fal fa-save me-1"></span>Grabar
                          </button>
                          <button type="button" class="dropdown-item border-bottom border-1"
                            @click="returnPage">
                            <span class="fal fa-ban me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                    <div v-if="canSave" class="d-none d-md-flex gap-2">
                      <button v-if="canSave" type="button" class="btn btn-sm btn-primary"
                        :disabled="isReadOnly" @click="saveProduct">
                        <span class="fal fa-save me-1"></span>Grabar
                      </button>
                      <button type="button" class="btn btn-warning btn-sm"
                        @click="returnPage">
                        <span class="fal fa-ban me-1"></span>Cancelar
                      </button>
                    </div>
                  </div>
                  <div class="col-4 col-md-4 text-md-end">
                    <button type="button" class="btn btn-danger btn-sm" @click="returnPage">
                      <span class="fal fa-arrow-alt-to-left me-1"></span>Volver
                    </button>
                  </div>
                </div>
              </div>

              <!-- Formulario -->
              <div class="panel-content pt-0">
                <form novalidate>

                  <!-- Sección 1: Identificación -->
                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-id-badge me-1"></i> Identificación del Producto
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="ProductCode">
                        Código del Producto <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="ProductCode"
                        name="ProductCode"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.ProductCode.$dirty && v$.ProductCode.$invalid }"
                        placeholder="Ej: PROD-001"
                        :disabled="isReadOnly"
                        autocomplete="off"
                        v-model.trim="v$.ProductCode.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar el código del Producto.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="BarCode">
                        Código de Barras <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-barcode"></i>
                        </span>
                        <input
                          type="text"
                          id="BarCode"
                          name="BarCode"
                          class="form-control"
                          :class="{ 'is-invalid': v$.BarCode.$dirty && v$.BarCode.$invalid }"
                          placeholder="Código de barras"
                          :disabled="isReadOnly"
                          autocomplete="off"
                          v-model.trim="v$.BarCode.$model"
                        />
                        <div class="invalid-feedback">Debe ingresar el código de barra.</div>
                      </div>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="ProductName">
                        Nombre del Producto <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="ProductName"
                        name="ProductName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.ProductName.$dirty && v$.ProductName.$invalid }"
                        placeholder="Nombre del Producto"
                        :disabled="isReadOnly"
                        autocomplete="off"
                        v-model.trim="v$.ProductName.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar el nombre del Producto.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Description">
                        Descripción <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="Description"
                        name="Description"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.Description.$dirty && v$.Description.$invalid }"
                        placeholder="Descripción del producto"
                        :disabled="isReadOnly"
                        autocomplete="off"
                        v-model.trim="v$.Description.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar la descripción del Producto.</small>
                    </div>
                  </div>

                  <!-- Sección 2: Clasificación -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-tags me-1"></i> Clasificación
                  </h6>
                  <div class="row">
                    <div class="col-md-4 mb-3">
                      <label class="form-label" for="laboratories">
                        Laboratorio / Proveedor
                      </label>
                      <select
                        class="form-select form-select-sm"
                        id="laboratories"
                        name="laboratories"
                        :disabled="isReadOnly"
                        v-model.trim="product.LaboratoryId"
                      >
                        <option value="">— Sin laboratorio —</option>
                        <option v-for="lab in laboratories" :value="lab.Id" :key="lab.Id">
                          {{ lab.LaboratoryName }}
                        </option>
                      </select>
                      <small class="text-muted">Opcional: no toda la mercadería tiene laboratorio.</small>
                    </div>
                    <div class="col-md-4 mb-3">
                      <label class="form-label" for="categories">
                        Categoría
                      </label>
                      <select
                        class="form-select form-select-sm"
                        id="categories"
                        name="categories"
                        :disabled="isReadOnly"
                        v-model.trim="product.CategoryId"
                      >
                        <option value="">— Seleccione una categoría —</option>
                        <option v-for="cat in categories" :value="cat.Id" :key="cat.Id">
                          {{ cat.CategoryName }}
                        </option>
                      </select>
                    </div>
                    <div class="col-md-4 mb-3">
                      <label class="form-label" for="units">
                        Unidad de Medida <span class="text-danger">*</span>
                      </label>
                      <select
                        class="form-select form-select-sm"
                        :class="{ 'is-invalid': v$.UomId.$dirty && v$.UomId.$invalid }"
                        id="units"
                        name="units"
                        :disabled="isReadOnly"
                        v-model.trim="v$.UomId.$model"
                      >
                        <option value="">— Seleccione una unidad —</option>
                        <option v-for="unit in unitsOfMeasurement" :value="unit.Id" :key="unit.Id">
                          {{ unit.UnitName }}
                        </option>
                      </select>
                      <small class="invalid-feedback">Debe seleccionar una unidad de medida.</small>
                    </div>
                  </div>

                  <!-- Sección 3: Precio y Stock -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-coins me-1"></i> Precio y Stock
                  </h6>
                  <div class="row">
                    <div class="col-sm-6 col-md-4 mb-3">
                      <label class="form-label d-block" for="precio">
                        Precio de Venta <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">Bs.</span>
                        <input
                          type="number"
                          id="precio"
                          name="precio"
                          class="form-control text-end"
                          :class="{ 'is-invalid': v$.SalePrice.$dirty && v$.SalePrice.$invalid }"
                          placeholder="0.00"
                          step="0.01"
                          min="0"
                          :disabled="isReadOnly"
                          v-model.trim="v$.SalePrice.$model"
                        />
                        <div class="invalid-feedback"
                          v-if="v$.SalePrice.$dirty && v$.SalePrice.required.$invalid">
                          {{ v$.SalePrice.required.$message }}
                        </div>
                        <div class="invalid-feedback"
                          v-else-if="v$.SalePrice.$dirty && v$.SalePrice.greaterThanZero.$invalid">
                          {{ v$.SalePrice.greaterThanZero.$message }}
                        </div>
                      </div>
                    </div>
                    <div class="col-sm-6 col-md-4 mb-3">
                      <label class="form-label d-block" for="cantidad">
                        Stock Actual
                        <i class="fal fa-lock ms-1 text-muted small"
                          title="Solo lectura — se actualiza por movimientos de inventario"></i>
                      </label>
                      <input
                        type="number"
                        id="cantidad"
                        name="cantidad"
                        class="form-control form-control-sm text-end"
                        :class="stockClass"
                        placeholder="0"
                        :disabled="true"
                        v-model.trim="v$.CurrentStock.$model"
                      />
                      <small
                        v-if="product.CurrentStock <= product.MinReorderQuantity && product.MinReorderQuantity > 0"
                        class="text-danger"
                      >
                        <i class="fal fa-exclamation-triangle me-1"></i>Por debajo del stock mínimo
                      </small>
                    </div>
                    <div class="col-sm-6 col-md-4 mb-3">
                      <label class="form-label d-block" for="cantidadReposicion">
                        Stock Mínimo de Reposición <span class="text-danger">*</span>
                      </label>
                      <input
                        type="number"
                        id="cantidadReposicion"
                        name="cantidadReposicion"
                        class="form-control form-control-sm text-end"
                        :class="{ 'is-invalid': v$.MinReorderQuantity.$dirty && v$.MinReorderQuantity.$invalid }"
                        placeholder="0"
                        min="0"
                        :disabled="isReadOnly"
                        v-model.trim="v$.MinReorderQuantity.$model"
                      />
                      <small class="invalid-feedback"
                        v-if="v$.MinReorderQuantity.$dirty && v$.MinReorderQuantity.required.$invalid">
                        {{ v$.MinReorderQuantity.required.$message }}
                      </small>
                      <small class="invalid-feedback"
                        v-else-if="v$.MinReorderQuantity.$dirty && v$.MinReorderQuantity.greaterThanZero.$invalid">
                        {{ v$.MinReorderQuantity.greaterThanZero.$message }}
                      </small>
                    </div>
                  </div>

                  <!-- Sección 4: Configuración -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-sliders-h me-1"></i> Configuración
                  </h6>
                  <div class="row">
                    <div class="col-sm-6 mb-3">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="AvailableInPos"
                          role="switch"
                          :disabled="isReadOnly"
                          v-model="v$.AvailableInPos.$model"
                        />
                        <label class="form-check-label" for="AvailableInPos">
                          Disponible en los puntos de venta
                        </label>
                      </div>
                    </div>
                    <div class="col-sm-6 mb-3">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="IsActive"
                          role="switch"
                          :disabled="isReadOnly"
                          v-model="product.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">
                          Producto activo
                        </label>
                      </div>
                    </div>
                    <div class="col-sm-6 mb-3">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="RequiresAuthorization"
                          role="switch"
                          :disabled="isReadOnly"
                          v-model="product.RequiresAuthorization"
                        />
                        <label class="form-check-label" for="RequiresAuthorization">
                          La venta requiere respaldo
                        </label>
                      </div>
                      <small class="text-muted">
                        Receta médica, permiso u otra autorización. Se avisa al vender.
                      </small>
                    </div>
                  </div>

                  <!--
                    Sección: datos del rubro farmacia.

                    Va en su propia sección y no mezclada con los campos del
                    núcleo porque es de un rubro: una ferretería no la usa. Solo
                    aparece en edición, igual que Trazabilidad, porque necesita
                    el producto ya creado para colgarle la composición.
                  -->
                  <template v-if="product.Id !== '0'">
                    <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                      <i class="fal fa-prescription-bottle-alt me-1"></i> Datos Farmacéuticos
                      <small class="fw-normal ms-1">(opcional)</small>
                    </h6>

                    <div class="row">
                      <div class="col-12 col-md-4 mb-3">
                        <label class="form-label d-block" for="FormId">Forma farmacéutica</label>
                        <select id="FormId" class="form-select form-select-sm"
                          :disabled="isReadOnly" v-model="pharma.FormId">
                          <option value="">— Sin especificar —</option>
                          <option v-for="f in formas" :key="f.Id" :value="f.Id">{{ f.Name }}</option>
                        </select>
                      </div>
                      <div class="col-12 col-md-4 mb-3">
                        <label class="form-label d-block" for="RouteId">Vía de administración</label>
                        <select id="RouteId" class="form-select form-select-sm"
                          :disabled="isReadOnly" v-model="pharma.RouteId">
                          <option value="">— Sin especificar —</option>
                          <option v-for="r in vias" :key="r.Id" :value="r.Id">{{ r.Name }}</option>
                        </select>
                      </div>
                      <div class="col-12 col-md-4 mb-3">
                        <label class="form-label d-block" for="ProductType">Tipo</label>
                        <select id="ProductType" class="form-select form-select-sm"
                          :disabled="isReadOnly" v-model="pharma.ProductType">
                          <option value="">— Sin especificar —</option>
                          <option value="generico">Genérico</option>
                          <option value="marca">Marca</option>
                          <option value="similar">Similar</option>
                        </select>
                      </div>
                      <div class="col-12 col-md-6 mb-3">
                        <label class="form-label d-block" for="Presentation">Presentación</label>
                        <input id="Presentation" type="text" class="form-control form-control-sm"
                          maxlength="150" placeholder="Ej: caja x 20 comprimidos"
                          :disabled="isReadOnly" v-model.trim="pharma.Presentation" />
                        <small class="text-muted">
                          Distingue dos productos con el mismo principio activo y concentración.
                        </small>
                      </div>
                      <div class="col-12 col-md-6 mb-3">
                        <label class="form-label d-block" for="DosageReference">
                          Posología de referencia
                        </label>
                        <input id="DosageReference" type="text" class="form-control form-control-sm"
                          maxlength="300" placeholder="Ej: Adultos: 1 comprimido cada 8 horas"
                          :disabled="isReadOnly" v-model.trim="pharma.DosageReference" />
                        <small class="text-muted">
                          Según prospecto. La dosis real depende del paciente y la indica quien receta.
                        </small>
                      </div>
                      <div class="col-12 col-md-6 mb-3">
                        <label class="form-label d-block" for="SanitaryRegistry">
                          Registro sanitario
                        </label>
                        <input id="SanitaryRegistry" type="text" class="form-control form-control-sm"
                          maxlength="60" placeholder="Nº de registro AGEMED"
                          :disabled="isReadOnly" v-model.trim="pharma.SanitaryRegistry" />
                      </div>
                      <div class="col-12 col-md-6 mb-3">
                        <label class="form-label d-block" for="SanitaryRegistryExpiry">
                          Vigencia del registro
                        </label>
                        <input id="SanitaryRegistryExpiry" type="date" class="form-control form-control-sm"
                          :disabled="isReadOnly" v-model="pharma.SanitaryRegistryExpiry" />
                        <small v-if="registroVencido" class="text-danger">
                          <i class="fal fa-exclamation-triangle me-1"></i>El registro está vencido.
                        </small>
                      </div>
                    </div>

                    <!--
                      Composición. Es una lista y no un campo de texto porque un
                      producto puede tener varios principios activos, cada uno con
                      su concentración — los antigripales casi siempre lo son. Es
                      lo que después permite encontrar equivalentes solo.
                    -->
                    <div class="d-flex align-items-center justify-content-between mb-2">
                      <label class="form-label mb-0">Composición</label>
                      <button v-if="!isReadOnly" type="button" class="btn btn-sm btn-outline-primary"
                        @click="agregarComponente">
                        <i class="fal fa-plus me-1"></i>Agregar componente
                      </button>
                    </div>

                    <div v-if="pharma.Components.length === 0" class="text-muted small mb-3">
                      Sin composición cargada. Al agregar los principios activos, el sistema
                      podrá ofrecer equivalentes de este producto sin que cargues nada más.
                    </div>

                    <div v-for="(c, ci) in pharma.Components" :key="ci" class="row g-2 mb-2 align-items-end">
                      <div class="col-12 col-md-5">
                        <label v-if="ci === 0" class="form-label small text-muted mb-1">Sustancia</label>
                        <input type="text" class="form-control form-control-sm"
                          list="lista-sustancias" maxlength="150"
                          placeholder="Ej: Ibuprofeno"
                          :disabled="isReadOnly"
                          :value="c.SubstanceName"
                          @input="onSustancia(c, $event)" />
                      </div>
                      <div class="col-6 col-md-2">
                        <label v-if="ci === 0" class="form-label small text-muted mb-1">Concentración</label>
                        <input type="number" step="0.0001" min="0" class="form-control form-control-sm text-end"
                          :disabled="isReadOnly" v-model.number="c.ConcentrationValue" />
                      </div>
                      <div class="col-6 col-md-2">
                        <label v-if="ci === 0" class="form-label small text-muted mb-1">Unidad</label>
                        <input type="text" class="form-control form-control-sm"
                          list="lista-unidades" maxlength="20" placeholder="mg"
                          :disabled="isReadOnly" v-model.trim="c.ConcentrationUnit" />
                      </div>
                      <div class="col-9 col-md-2">
                        <div class="form-check form-switch mb-1">
                          <input class="form-check-input" type="checkbox" role="switch"
                            :id="'activo-' + ci" :disabled="isReadOnly"
                            v-model="c.IsActiveIngredient" />
                          <label class="form-check-label small" :for="'activo-' + ci">
                            {{ c.IsActiveIngredient ? 'Principio activo' : 'Excipiente' }}
                          </label>
                        </div>
                      </div>
                      <div class="col-3 col-md-1 text-end">
                        <button v-if="!isReadOnly" type="button" class="btn btn-sm btn-outline-danger"
                          title="Quitar" @click="pharma.Components.splice(ci, 1)">
                          <i class="fal fa-trash-alt"></i>
                        </button>
                      </div>
                    </div>

                    <datalist id="lista-sustancias">
                      <option v-for="s in sustancias" :key="s.Id" :value="s.SubstanceName"></option>
                    </datalist>
                    <datalist id="lista-unidades">
                      <option v-for="u in UNIDADES_CONCENTRACION" :key="u" :value="u"></option>
                    </datalist>

                    <small class="text-muted d-block mb-3">
                      Si la sustancia no está en la lista, escribila igual: se da de alta al guardar.
                    </small>

                    <!--
                      Prospecto. Colapsado y cargado bajo demanda: son varios KB
                      y la mayoría de los productos no lo tiene. Traerlo con la
                      ficha desperdiciaría justamente lo que se ganó al ponerlo
                      en una tabla aparte.
                    -->
                    <div class="border rounded p-2 mb-3">
                      <div class="d-flex align-items-center justify-content-between">
                        <div>
                          <button type="button" class="btn btn-sm btn-link text-decoration-none p-0"
                            @click="alternarProspecto">
                            <i class="fal me-1" :class="prospectoAbierto ? 'fa-chevron-down' : 'fa-chevron-right'"></i>
                            Prospecto
                          </button>
                          <span v-if="!prospectoAbierto && prospectoCargado && !prospecto"
                            class="text-muted small ms-2">sin cargar</span>
                          <span v-else-if="!prospectoAbierto && prospecto"
                            class="badge bg-success-subtle text-success-emphasis border border-success-subtle ms-2">
                            cargado
                          </span>
                        </div>
                        <div v-if="prospectoAbierto" class="btn-group btn-group-sm">
                          <button type="button" class="btn"
                            :class="verPrevia ? 'btn-outline-secondary' : 'btn-secondary'"
                            @click="verPrevia = false">Editar</button>
                          <button type="button" class="btn"
                            :class="verPrevia ? 'btn-secondary' : 'btn-outline-secondary'"
                            @click="verPrevia = true">Vista previa</button>
                        </div>
                      </div>

                      <div v-if="prospectoAbierto" class="mt-2">
                        <textarea v-if="!verPrevia" class="form-control form-control-sm font-monospace"
                          rows="10" :disabled="isReadOnly"
                          placeholder="Pegá el prospecto acá. Se respetan los saltos de línea, y podés usar formato: ## Título, - viñeta, **negrita**."
                          v-model="prospecto"></textarea>

                        <div v-else class="border rounded p-3 bg-body-tertiary prospecto-previa">
                          <div v-if="prospecto" v-html="prospectoHtml"></div>
                          <span v-else class="text-muted">Sin contenido.</span>
                        </div>

                        <small class="text-muted d-block mt-1">
                          Información del prospecto del fabricante. Vaciar el texto elimina el prospecto.
                        </small>
                      </div>
                    </div>

                    <!--
                      Alternativas definidas a mano. Solo para lo que NO se puede
                      deducir: la opción más económica, la que el cliente suele
                      preferir. Los equivalentes por composición aparecen abajo
                      y no se cargan acá.
                    -->
                    <div class="d-flex align-items-center justify-content-between mb-2">
                      <label class="form-label mb-0">Alternativas sugeridas</label>
                      <button v-if="!isReadOnly" type="button" class="btn btn-sm btn-outline-primary"
                        @click="abrirSelectorAlternativa()">
                        <i class="fal fa-plus me-1"></i>Sugerir alternativa
                      </button>
                    </div>

                    <p v-if="alternativasManuales.length === 0" class="text-muted small mb-2">
                      Todavía no hay ninguna. Se ofrecen en el punto de venta cuando
                      alguien pide este producto.
                    </p>

                    <div v-for="a in alternativasManuales" :key="a.ProductId"
                      class="d-flex align-items-center justify-content-between border rounded p-2 mb-1">
                      <div>
                        <span class="fw-semibold small">{{ a.ProductName }}</span>
                        <small class="text-muted d-block">
                          <span v-if="a.Reason">{{ a.Reason }} · </span>Bs. {{ formatCurrency(a.SalePrice) }}
                        </small>
                      </div>
                      <button v-if="!isReadOnly" type="button" class="btn btn-sm btn-outline-danger"
                        title="Quitar" @click="quitarAlternativa(a.ProductId)">
                        <i class="fal fa-trash-alt"></i>
                      </button>
                    </div>

                    <!-- Equivalentes: no se cargan, se deducen de la composición. -->
                    <div v-if="equivalentesAutomaticos.length > 0" class="alert alert-info py-2 mb-3">
                      <div class="mb-1">
                        <i class="fal fa-exchange me-1"></i>
                        <strong>{{ equivalentesAutomaticos.length }}</strong> producto(s) con esta misma composición.
                        El punto de venta ya los ofrece sin que cargues nada.
                      </div>
                      <!--
                        Atajo: el caso más común es querer destacar justamente al
                        equivalente más barato. Buscarlo a mano cuando el sistema
                        ya sabe cuál es sería hacerle repetir el trabajo.
                      -->
                      <div class="d-flex flex-wrap gap-2 mt-2">
                        <div v-for="e in equivalentesAutomaticos" :key="e.ProductId"
                          class="d-flex align-items-center gap-2 border rounded px-2 py-1 bg-body">
                          <span class="small">
                            {{ e.ProductName }}
                            <span class="text-muted">({{ formatCurrency(e.SalePrice) }})</span>
                          </span>
                          <button v-if="!isReadOnly" type="button" class="btn btn-sm btn-outline-primary py-0"
                            title="Destacarlo como sugerencia en el punto de venta"
                            @click="sugerirEquivalente(e)">
                            Sugerir
                          </button>
                        </div>
                      </div>
                    </div>
                  </template>

                  <!-- Sección 5: Trazabilidad -->
                  <!--
                    Solo en edición: activar lotes actúa sobre el stock existente,
                    y un producto que todavía no se grabó no tiene ninguno.
                  -->
                  <template v-if="product.Id !== '0'">
                    <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                      <i class="fal fa-layer-group me-1"></i> Trazabilidad
                    </h6>
                    <div class="row">
                      <div class="col-12 col-md-4 mb-3">
                        <label class="form-label d-block">Seguimiento de existencias</label>
                        <span :class="trackingBadge">{{ trackingLabel }}</span>
                      </div>
                      <div class="col-12 col-md-8 mb-3">
                        <template v-if="product.TrackingMode === 'none'">
                          <div class="d-flex flex-wrap gap-2">
                            <button
                              type="button"
                              class="btn btn-sm btn-outline-primary"
                              :disabled="isReadOnly"
                              @click="activateTrackingMode('lot')"
                            >
                              <i class="fal fa-layer-group me-1"></i>Activar control por lotes
                            </button>
                            <button
                              type="button"
                              class="btn btn-sm btn-outline-primary"
                              :disabled="isReadOnly"
                              @click="activateTrackingMode('serial')"
                            >
                              <i class="fal fa-barcode me-1"></i>Activar control por series
                            </button>
                          </div>
                          <small class="d-block text-muted mt-2">
                            <strong>Lotes</strong> para lo que llega en cajas con un mismo código y
                            vencimiento: medicamentos. <strong>Series</strong> para lo que se
                            identifica unidad por unidad: tensiómetros, nebulizadores y todo lo que
                            lleva garantía.
                          </small>
                          <small class="d-block text-muted mt-1">
                            Las {{ product.CurrentStock }} unidades que hay hoy quedan como
                            existencia sin identificar y se venden primero.
                            <strong>No se puede deshacer.</strong>
                          </small>
                        </template>
                        <small v-else-if="product.TrackingMode === 'lot'" class="text-muted">
                          <i class="fal fa-check-circle text-success me-1"></i>
                          Cada recepción registra su lote y vencimiento; la venta consume primero
                          lo que vence antes.
                        </small>
                        <small v-else class="text-muted">
                          <i class="fal fa-check-circle text-success me-1"></i>
                          Cada recepción registra un número de serie por unidad, y la venta deja
                          asentado cuál se entregó.
                        </small>
                      </div>
                    </div>
                  </template>

                </form>
              </div>

            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <AlternativePickerModal
    :visible="selectorAbierto"
    :product-id="product.Id"
    :product-name="product.ProductName"
    :product-price="product.SalePrice"
    :exclude-ids="idsExcluidos"
    :preselected="preseleccion"
    :preselected-reason="motivoPreseleccion"
    @close="selectorAbierto = false"
    @confirm="confirmarAlternativa"
  />
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from "vue-router";
import useVuelidate from '@vuelidate/core';
import { helpers, required } from '@vuelidate/validators';
import utils from '@/utils/msg';

import { Product } from '@/modules/inventory/models/product.model';
import { Laboratory } from '@/modules/inventory/models/laboratory.model';
import { Category } from '@/modules/inventory/models/category.model';
import { UnitOfMeasurement } from '@/modules/inventory/models/unitOfMeasurement.model';

import useProduct from '@/modules/inventory/composables/useProduct';
import AlternativePickerModal from '@/modules/inventory/components/AlternativePickerModal.vue';
import usePharma from '@/modules/inventory/composables/usePharma';
import { todayIso } from '@/utils/dateHelper';
import { renderMarkdown } from '@/utils/markdown';
import {
  ProductPharma, ProductComponent, UNIDADES_CONCENTRACION,
  type PharmaCatalogItem, type PharmaSubstance, type ProductEquivalent,
} from '@/modules/inventory/models/pharma.model';
import useLaboratory from '@/modules/inventory/composables/useLaboratory';
import useCategory from '@/modules/inventory/composables/useCategory';
import useUnitOfMeasurement from '@/modules/inventory/composables/useUnitOfMeasurement';
import usePermissions from '@/modules/common/composables/usePermissions';

const router = useRouter();

// La búsqueda de alternativas se mudó al selector, que la resuelve por su cuenta.
const { getProductById, updateProduct, createProduct, activateTracking } = useProduct();
const { getLaboratories: fetchLaboratories } = useLaboratory();
const { getCategories: fetchCategories } = useCategory();
const { getUnitsOfMeasurement: fetchUnitsOfMeasurement } = useUnitOfMeasurement();

const product = ref(new Product());
const laboratories = ref<Laboratory[]>([]);
const categories = ref<Category[]>([]);
const unitsOfMeasurement = ref<UnitOfMeasurement[]>([]);

// Validador personalizado para mayor a cero
const greaterThanZero = (value: number) => value > 0;

const rules = {
  ProductCode: { required },
  BarCode: { required },
  ProductName: { required },
  Description: { required },
  SalePrice: {
    required: helpers.withMessage('Debe ingresar el precio del Producto.', required),
    greaterThanZero: helpers.withMessage('El valor debe ser mayor a cero', greaterThanZero),
  },
  CurrentStock: { required },
  MinReorderQuantity: {
    required: helpers.withMessage('Debe ingresar la cantidad mínima de reposición.', required),
    greaterThanZero: helpers.withMessage('El valor debe ser mayor a cero', greaterThanZero),
  },
  UomId: { required },
  AvailableInPos: { required },
};
const v$ = useVuelidate(rules, product);

try {
  product.value.Id = router.currentRoute.value.params.id
    ? router.currentRoute.value.params.id.toString()
    : '0';
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
} catch (error) {
  product.value.Id = '0';
}

const isSaved = ref(false);

const { can } = usePermissions();
// Permiso efectivo para grabar: crear si es nuevo, actualizar si es edición.
const canSave = computed(() =>
  product.value.Id === '0'
    ? can('products-admin', 'create')
    : can('products-admin', 'update')
);
// El formulario queda bloqueado si ya se grabó o si el usuario no puede grabar.
const isReadOnly = computed(() => isSaved.value || !canSave.value);

const { getForms, getRoutes, searchSubstances, getByProduct: getPharma, savePharma, getLeaflet, saveLeaflet, getEquivalents, addAlternative, removeAlternative } = usePharma();

const pharma = ref(new ProductPharma());
const formas = ref<PharmaCatalogItem[]>([]);
const vias = ref<PharmaCatalogItem[]>([]);
const sustancias = ref<PharmaSubstance[]>([]);
const equivalentes = ref<ProductEquivalent[]>([]);

/** Un registro vencido no debería venderse; se avisa al editar. */
const registroVencido = computed(() =>
  !!pharma.value.SanitaryRegistryExpiry && pharma.value.SanitaryRegistryExpiry < todayIso()
);

/** Solo para el aviso de equivalentes; el formato del resto lo maneja el input. */
const formatCurrency = (val: number): string =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const agregarComponente = () => pharma.value.Components.push(new ProductComponent());

const equivalentesAutomaticos = computed(() => equivalentes.value.filter(e => !e.IsManual));

/** Solo las manuales: las automáticas se muestran aparte y no se administran. */
const alternativasManuales = computed(() => equivalentes.value.filter(e => e.IsManual));

// ── Selector de alternativas ───────────────────────────────
const selectorAbierto = ref(false);
const preseleccion = ref<Product | null>(null);
const motivoPreseleccion = ref('');

/**
 * Lo que el selector NO debe ofrecer: lo ya sugerido y lo que el sistema deduce
 * solo por composición. Ofrecerlos sería invitar a duplicar algo resuelto.
 */
const idsExcluidos = computed(() => equivalentes.value.map(e => e.ProductId));

const abrirSelectorAlternativa = (
  producto: Product | null = null,
  motivo = '',
) => {
  preseleccion.value = producto;
  motivoPreseleccion.value = motivo;
  selectorAbierto.value = true;
};

/**
 * Atajo desde los equivalentes automáticos: se abre el selector con ese producto
 * ya elegido. No se agrega de una porque el motivo es lo que el vendedor lee, y
 * conviene confirmarlo antes de fijarlo.
 */
const sugerirEquivalente = (e: ProductEquivalent) => {
  const comoProducto = {
    Id: e.ProductId,
    ProductName: e.ProductName,
    SalePrice: e.SalePrice,
    CurrentStock: e.CurrentStock,
  } as Product;
  abrirSelectorAlternativa(comoProducto, 'Misma composición');
};

const confirmarAlternativa = async (payload: { productId: string; reason: string }) => {
  const { ok } = await addAlternative(product.value.Id, payload.productId, payload.reason);
  selectorAbierto.value = false;
  if (!ok) return;

  const eq = await getEquivalents(product.value.Id);
  if (eq.ok) equivalentes.value = eq.Data;
};

const quitarAlternativa = async (alternativeId: string) => {
  const { ok } = await removeAlternative(product.value.Id, alternativeId);
  if (!ok) return;
  const eq = await getEquivalents(product.value.Id);
  if (eq.ok) equivalentes.value = eq.Data;
};

const prospecto = ref('');
const prospectoAbierto = ref(false);
const prospectoCargado = ref(false);
const verPrevia = ref(false);

const prospectoHtml = computed(() => renderMarkdown(prospecto.value));

/**
 * Se trae la primera vez que se abre, no al cargar la ficha: es lo que hace
 * que el peso del prospecto no lo pague quien solo viene a cambiar un precio.
 */
const alternarProspecto = async () => {
  prospectoAbierto.value = !prospectoAbierto.value;
  if (!prospectoAbierto.value || prospectoCargado.value) return;

  const { ok, Data } = await getLeaflet(product.value.Id);
  if (ok) prospecto.value = Data ?? '';
  prospectoCargado.value = true;
};

/**
 * Al escribir la sustancia se busca el id en el catálogo. Si no coincide con
 * ninguna, queda solo el nombre y el servidor la da de alta al guardar: cargar
 * un producto no debería obligar a salir a crear el principio activo primero.
 */
const onSustancia = (componente: ProductComponent, event: Event) => {
  const nombre = (event.target as HTMLInputElement).value;
  componente.SubstanceName = nombre;

  const conocida = sustancias.value.find(
    s => s.SubstanceName.toUpperCase() === nombre.trim().toUpperCase()
  );
  componente.SubstanceId = conocida?.Id ?? '';
};

/** Los catálogos se traen una vez; el listado de sustancias alimenta el datalist. */
const cargarRubroFarmacia = async (productId: string) => {
  const [f, v, s] = await Promise.all([getForms(), getRoutes(), searchSubstances('')]);
  if (f.ok) formas.value = f.Data;
  if (v.ok) vias.value = v.Data;
  if (s.ok) sustancias.value = s.Data;

  const { ok, Data } = await getPharma(productId);
  if (ok && Data) {
    pharma.value = Object.assign(new ProductPharma(), Data, {
      FormId: Data.FormId ?? '',
      RouteId: Data.RouteId ?? '',
      Presentation: Data.Presentation ?? '',
      DosageReference: Data.DosageReference ?? '',
      ProductType: Data.ProductType ?? '',
      SanitaryRegistry: Data.SanitaryRegistry ?? '',
      SanitaryRegistryExpiry: (Data.SanitaryRegistryExpiry ?? '').toString().substring(0, 10),
      Components: (Data.Components ?? []).map(c => Object.assign(new ProductComponent(), c, {
        ConcentrationUnit: c.ConcentrationUnit ?? '',
      })),
    });
  }

  const eq = await getEquivalents(productId);
  if (eq.ok) equivalentes.value = eq.Data;
};

const trackingLabel = computed(() => {
  if (product.value.TrackingMode === 'lot') return 'Por lotes';
  if (product.value.TrackingMode === 'serial') return 'Por número de serie';
  return 'Sin seguimiento';
});

/**
 * Mismo criterio de contraste que el resto de la app: `subtle` + `emphasis` son
 * las variantes que el bloque de tema oscuro redefine.
 */
const trackingBadge = computed(() => {
  const base = 'badge border';
  if (product.value.TrackingMode === 'none')
    return `${base} bg-secondary-subtle text-secondary-emphasis border-secondary-subtle`;
  return `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
});

/**
 * La activación es irreversible, así que se confirma con las consecuencias a la
 * vista y se recarga la ficha desde el servidor: el modo lo decide él.
 */
const activateTrackingMode = async (modo: 'lot' | 'serial') => {
  const porLotes = modo === 'lot';
  const que = porLotes ? 'el lote' : 'un número de serie por unidad';

  const confirmado = await utils.showMessageQuestion(
    `¿Activar el control por ${porLotes ? 'lotes' : 'números de serie'} en ` +
    `"${product.value.ProductName}"? Desde ahora cada recepción pedirá ${que}. ` +
    'Esta acción no se puede deshacer.'
  );
  if (!confirmado) return;

  const { ok } = await activateTracking(product.value.Id, modo);
  if (!ok) return;

  await getProductXId(product.value.Id);
  await utils.showMessageModal({
    Description: porLotes
      ? 'El producto ahora se controla por lotes.'
      : 'El producto ahora se controla por números de serie.',
    MessageType: 'success',
  });
};

// Clase dinámica para el campo Stock Actual según nivel vs mínimo
const stockClass = computed((): string => {
  if (!product.value.MinReorderQuantity) return '';
  if (product.value.CurrentStock === 0) return 'text-danger fw-bold';
  if (product.value.CurrentStock <= product.value.MinReorderQuantity) return 'text-danger';
  if (product.value.CurrentStock <= product.value.MinReorderQuantity * 1.5) return 'text-warning';
  return 'text-success';
});

onMounted(async () => {
  if (product.value.Id !== '0') {
    await getProductXId(product.value.Id);
    await cargarRubroFarmacia(product.value.Id);
  }
  await getLaboratories();
  await getCategories();
  await getUnitsOfMeasurement();
});

const getProductXId = async (productId: string) => {
  const { ok, Data: productResp } = await getProductById(productId);
  if (!ok) return;

  product.value = productResp;
  // Laboratorio y categoría son opcionales y llegan con null. El `<select>`
  // compara contra el value="" de su opción «sin asignar»: sin esto no quedaría
  // ninguna seleccionada y el campo se vería vacío en vez de explícitamente
  // sin asignar.
  product.value.LaboratoryId = productResp.LaboratoryId ?? '';
  product.value.CategoryId = productResp.CategoryId ?? '';
};

const getLaboratories = async () => {
  const { ok, Data: laboratoriesResp } = await fetchLaboratories('');
  if (ok) laboratories.value = laboratoriesResp;
};

const getCategories = async () => {
  const { ok, Data: categoriesResp } = await fetchCategories('');
  if (ok) categories.value = categoriesResp;
};

const getUnitsOfMeasurement = async () => {
  const { ok, Data: unitsResp } = await fetchUnitsOfMeasurement('');
  if (ok) unitsOfMeasurement.value = unitsResp;
};

const returnPage = () => {
  router.push({ name: 'products-admin' });
};

const saveProduct = async () => {
  if (!canSave.value) {
    await utils.showMessageModal({ Description: 'No tiene permiso para guardar productos.', MessageType: 'warning' });
    return;
  }
  if (!v$.value.$invalid) {
    const respuesta = await utils.showMessageQuestion('¿Desea guardar el producto?');
    if (respuesta) {
      if (product.value.Id === '0') {
        const { ok, Data: idProduct } = await createProduct(product.value);
        if (ok) {
          isSaved.value = ok;
          product.value.Id = idProduct;
          await utils.showMessageModal({ Description: 'El producto se creó correctamente.', MessageType: 'success' });
        }
      } else {
        const { ok, Data: okResp } = await updateProduct(product.value);
        if (ok) {
          // Los datos del rubro se guardan aparte —son de otro endpoint— pero
          // detrás de un solo botón: para quien carga, es una sola acción.
          // Si esto fallara, el producto ya quedó guardado y el error se ve.
          const guardado = await savePharma(product.value.Id, pharma.value);

          // Solo si se abrió la sección: si nadie la tocó, `prospecto` está
          // vacío y guardarlo borraría el que ya existía en la base.
          if (prospectoCargado.value) {
            await saveLeaflet(product.value.Id, prospecto.value);
          }

          isSaved.value = okResp;
          if (okResp && guardado.ok) {
            await utils.showMessageModal({ Description: 'El producto se actualizó correctamente.', MessageType: 'success' });
            // La composición pudo cambiar, y con ella los equivalentes.
            const eq = await getEquivalents(product.value.Id);
            if (eq.ok) equivalentes.value = eq.Data;
          }
        }
      }
    }
  } else {
    v$.value.$touch();
  }
};
</script>

<style scoped>
/* El prospecto usa títulos de markdown; sin esto un `##` se vería enorme
   dentro de un formulario. */
.prospecto-previa :deep(h1),
.prospecto-previa :deep(h2),
.prospecto-previa :deep(h3) { font-size: 1rem; font-weight: 600; margin-top: .75rem; }
.prospecto-previa :deep(p) { margin-bottom: .5rem; }
.prospecto-previa :deep(ul) { margin-bottom: .5rem; padding-left: 1.25rem; }
</style>
