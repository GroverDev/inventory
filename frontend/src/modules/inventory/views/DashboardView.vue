<template>
  <div class="content-wrapper">
    <h1 class="subheader-title mb-2">Dashboard, Inventarios</h1>
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-0">
        <li class="breadcrumb-item">Inicio</li>
        <li class="breadcrumb-item active" aria-current="page">Tablero Información</li>
      </ol>
    </nav>
    <h1 class="subheader-title mt-4">
      <small> Información importante
      </small>
    </h1>

    <div class="main-content">

      <div class="row">
        <div class="col-sm-12">
          <div class="card shadow-0 mb-g">
            <div class="card-header">
              <div class="card-title">Venta Diaria</div>
            </div>
            <div class="card-body">
              <p class="card-text">PRoductos Vendidos.</p>
              <div class="table-responsive">
                <table class="table table-bordered table-striped table-hover mb-0">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Phone</th>
                      <th>Address</th>
                      <th>City</th>
                      <th>State</th>
                      <th>Zip</th>
                      <th>Country</th>
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>John Doe</td>
                      <td>john.doe@example.com</td>
                      <td>123-456-7890</td>
                      <td>123 Main St</td>
                      <td>Anytown</td>
                      <td>CA</td>
                      <td>12345</td>
                      <td>USA</td>
                      <td>Edit</td>
                    </tr>
                    <tr>
                      <td>Jane Smith</td>
                      <td>jane.smith@example.com</td>
                      <td>987-654-3210</td>
                      <td>123 Main St</td>
                      <td>Anytown</td>
                      <td>CA</td>
                      <td>12345</td>
                      <td>USA</td>
                      <td>Edit</td>
                    </tr>
                    <tr>
                      <td>Bob Johnson</td>
                      <td>bob.johnson@example.com</td>
                      <td>555-123-4567</td>
                      <td>123 Main St</td>
                      <td>Anytown</td>
                      <td>CA</td>
                      <td>12345</td>
                      <td>USA</td>
                      <td>Edit</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

</template>

<script setup lang="ts">

</script>

<style scoped></style>
