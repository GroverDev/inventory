<template>
  <header class="app-header">
    <!-- Collapse icon -->
    <div class="d-flex flex-grow-1 w-100 me-auto align-items-center">

      <!-- App logo -->
      <a href="#" class="app-logo flex-shrink-0" data-prefix="v5.2.0" data-action="playsound">
        <img src="/assets/img/logo.png" alt="logo">

        <!-- <svg class="custom-logo">
          <use href="/assets/img/app-logo.svg#custom-logo"></use>
        </svg> -->

        <!-- Logo Backdrop Animation -->
        <div class="logo-backdrop">
          <div class="blobs">
            <svg viewbox="0 0 1200 1200">
              <g class="blob blob-1">
                <path d="M 100 600 q 0 -700, 500 -500 t 500 500 t -500 500 T 100 600 z" />
              </g>
              <g class="blob blob-2">
                <path d="M 100 600 q -50 -400, 500 -500 t 450 550 t -500 500 T 100 600 z" />
              </g>
              <g class="blob blob-3">
                <path d="M 100 600 q 0 -400, 500 -500 t 400 500 t -500 500 T 100 600 z" />
              </g>
              <g class="blob blob-4">
                <path d="M 150 600 q 0 -600, 500 -500 t 500 550 t -500 500 T 150 600 z" />
              </g>
              <g class="blob blob-1 alt">
                <path d="M 150 600 q 0 -600, 500 -500 t 500 550 t -500 500 T 150 600 z" />
              </g>
              <g class="blob blob-2 alt">
                <path d="M 100 600 q 100 -600, 500 -500 t 400 500 t -500 500 T 100 600 z" />
              </g>
              <g class="blob blob-3 alt">
                <path d="M 100 600 q 0 -400, 500 -500 t 400 500 t -500 500 T 100 600 z" />
              </g>
              <g class="blob blob-4 alt">
                <path d="M 150 600 q 0 -600, 500 -500 t 500 550 t -500 500 T 150 600 z" />
              </g>
            </svg>
          </div>
        </div>
      </a>

      <button class="mobile-menu-icon me-2 d-flex d-sm-flex d-md-flex d-lg-none flex-shrink-0" @click="layoutStore.toggleMobileMenu()" aria-label="Toggle Mobile Menu">
        <svg class="sa-icon">
          <use href="/assets/icons/sprite.svg#menu"></use>
        </svg>
      </button>

      <!-- Collapse icon -->
      <button type="button" class="collapse-icon me-3 d-none d-lg-inline-flex d-xl-inline-flex d-xxl-inline-flex"
        @click="layoutStore.toggleNavMinified()" aria-label="Toggle Navigation Size">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 5 8">
          <polygon fill="#878787" points="4.5,1 3.8,0.2 0,4 3.8,7.8 4.5,7 1.5,4" />
        </svg>
      </button>


    </div>

    <!-- Settings -->
    <button type="button" class="btn btn-system hidden-mobile" @click="layoutStore.toggleSettingsDrawer()" aria-label="Open Settings">
      <svg class="sa-icon sa-icon-2x">
        <use href="/assets/icons/sprite.svg#settings"></use>
      </svg>
    </button>

    <!-- Theme modes -->
    <button type="button" class="btn btn-system" @click="themeStore.toggleTheme()" aria-label="Toggle Dark Mode"
      :aria-pressed="themeStore.theme === 'dark'">
      <svg class="sa-icon sa-icon-2x sa-mode-light">
        <use href="/assets/icons/sprite.svg#sun"></use>
      </svg>
      <svg class="sa-icon sa-icon-2x sa-mode-dark">
        <use href="/assets/icons/sprite.svg#moon"></use>
      </svg>
    </button>

    <!-- Sidebar -->
    <button type="button" class="btn btn-system d-none d-sm-block d-sm-none d-md-none d-lg-block"
      @click="layoutStore.toggleAppDrawer()" aria-label="Open Sidebar">
      <svg class="sa-icon sa-icon-2x">
        <use href="/assets/icons/sprite.svg#aperture"></use>
      </svg>
    </button>

    <!-- Full Screen Mode -->
    <button type="button" class="btn btn-system d-none d-sm-block d-sm-none d-md-none d-lg-block"
      @click="toggleFullscreen()" aria-label="Toggle Fullscreen">
      <svg class="sa-icon sa-icon-2x sa-fullscreen-on">
        <use href="/assets/icons/sprite.svg#minimize"></use>
      </svg>
      <svg class="sa-icon sa-icon-2x sa-fullscreen-off">
        <use href="/assets/icons/sprite.svg#maximize"></use>
      </svg>
    </button>

    <!-- Notifications -->
    <button type="button" class="btn btn-system dropdown-toggle no-arrow" data-bs-toggle="dropdown"
      aria-expanded="false" aria-label="Open Notifications">
      <span class="badge badge-icon pos-top pos-end">5</span>
      <svg class="sa-icon sa-icon-2x">
        <use href="/assets/icons/sprite.svg#bell"></use>
      </svg>
    </button>

    <!-- Notifications dropdown -->
    <div class="dropdown-menu dropdown-menu-animated dropdown-xl dropdown-menu-end p-0">
      <div class="notification-header rounded-top mb-2">
        <h4 class="m-0">
          5 New
          <small class="mb-0 opacity-80">User Notifications</small>
        </h4>
      </div>

      <ul class="nav nav-tabs nav-tabs-clean" role="tablist">
        <li class="nav-item d-none">
          <a class="nav-link active" data-bs-toggle="tab" href="#tab-default" role="tab" aria-selected="true">Hidden</a>
        </li>
        <li class="nav-item">
          <a class="nav-link px-4 fs-md fw-500" data-bs-toggle="tab" href="#tab-messages" role="tab"
            aria-selected="false">Messages</a>
        </li>
        <li class="nav-item">
          <a class="nav-link px-4 fs-md fw-500" data-bs-toggle="tab" href="#tab-feeds" role="tab"
            aria-selected="false">Feeds</a>
        </li>
        <li class="nav-item">
          <a class="nav-link px-4 fs-md fw-500" data-bs-toggle="tab" href="#tab-events" role="tab"
            aria-selected="false">Events</a>
        </li>
      </ul>

      <div class="tab-content tab-notification">
        <!--Security message-->
        <div class="tab-pane fade show active" id="tab-default" role="tabpanel">
          <div class="d-flex h-100">
            <div class="px-4 d-flex flex-column align-items-center justify-content-center">

              <svg class="sa-icon sa-icon-5x sa-icon-primary">
                <use href="/assets/icons/sprite.svg#arrow-up-circle"></use>
              </svg>
              <span class="text-center fw-300" style="font-size: 1.25rem;">
                Select a tab above
              </span>
              <div class="mb-0 py-3 text-center fs-md fw-300 text-muted">
                This blank page helps protect your privacy.
                To change this default message, <a href="#">update your settings</a>.
              </div>
            </div>
          </div>
        </div>
        <!-- Messages -->
        <div class="tab-pane fade" id="tab-messages" role="tabpanel">
          <div class="custom-scroll h-100">
            <ul class="notification">
              <li class="unread alert alert-dismissable">
                <div class="d-flex align-items-center">
                  <span class="status me-2">
                    <span class="profile-image rounded-circle d-inline-block"
                      style="background-image:url('/assets/img/demo/avatars/avatar-c.png')"></span>
                  </span>
                  <span class="d-flex flex-column flex-1 ms-1">
                    <!-- <span class="name">Melissa Ayre <span class="badge bg-primary fw-n position-absolute top-0 end-0 mt-1">INBOX</span></span> -->
                    <span class="name">Melissa Ayre</span>
                    <span class="msg-a fs-sm">Re: New security codes</span>
                    <span class="msg-b fs-xs">Hello again and thanks for being part...</span>
                    <span class="fs-nano text-muted mt-1">56 seconds ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="unread alert alert-dismissable">
                <div class="d-flex align-items-center">
                  <span class="status me-2">
                    <span class="profile-image rounded-circle d-inline-block"
                      style="background-image:url('/assets/img/demo/avatars/avatar-a.png')"></span>
                  </span>
                  <span class="d-flex flex-column flex-1 ms-1">
                    <span class="name">Adison Lee</span>
                    <span class="msg-a fs-sm">Msed quia non numquam eius</span>
                    <span class="fs-nano text-muted mt-1">2 minutes ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center">
                  <span class="status status-success me-2">
                    <span class="profile-image rounded-circle d-inline-block"
                      style="background-image:url('/assets/img/demo/avatars/avatar-b.png')"></span>
                  </span>
                  <span class="d-flex flex-column flex-1 ms-1">
                    <span class="name">Oliver Kopyuv</span>
                    <span class="msg-a fs-sm">Msed quia non numquam eius</span>
                    <span class="fs-nano text-muted mt-1">3 days ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center">
                  <span class="status status-warning me-2">
                    <span class="profile-image rounded-circle d-inline-block"
                      style="background-image:url('/assets/img/demo/avatars/avatar-e.png')"></span>
                  </span>
                  <span class="d-flex flex-column flex-1 ms-1">
                    <span class="name">Dr. John Cook PhD</span>
                    <span class="msg-a fs-sm">Msed quia non numquam eius</span>
                    <span class="fs-nano text-muted mt-1">2 weeks ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center">
                  <span class="status status-success me-2">
                    <span class="profile-image rounded-circle d-inline-block"
                      style="background-image:url('/assets/img/demo/avatars/avatar-h.png')"></span>
                  </span>
                  <span class="d-flex flex-column flex-1 ms-1">
                    <span class="name">Sarah McBrook</span>
                    <span class="msg-a fs-sm">Msed quia non numquam eius</span>
                    <span class="fs-nano text-muted mt-1">3 weeks ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center">
                  <span class="status status-success me-2">
                    <span class="profile-image rounded-circle d-inline-block"
                      style="background-image:url('/assets/img/demo/avatars/avatar-m.png')"></span>
                  </span>
                  <span class="d-flex flex-column flex-1 ms-1">
                    <span class="name">Anothony Bezyeth</span>
                    <span class="msg-a fs-sm">Msed quia non numquam eius</span>
                    <span class="fs-nano text-muted mt-1">one month ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center">
                  <span class="status status-danger me-2">
                    <span class="profile-image rounded-circle d-inline-block"
                      style="background-image:url('/assets/img/demo/avatars/avatar-j.png')"></span>
                  </span>
                  <span class="d-flex flex-column flex-1 ms-1">
                    <span class="name">Lisa Hatchensen</span>
                    <span class="msg-a fs-sm">Msed quia non numquam eius</span>
                    <span class="fs-nano text-muted mt-1">one year ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
            </ul>
            <div class="notification-empty-msg">
              <svg class="sa-icon sa-icon-5x sa-icon-primary">
                <use href="/assets/icons/sprite.svg#coffee"></use>
              </svg>
              <span>
                No new messages
              </span>
            </div>
          </div>
        </div>
        <!-- Feeds -->
        <div class="tab-pane fade" id="tab-feeds" role="tabpanel">
          <div class="custom-scroll h-100">
            <ul class="notification">
              <li class="unread alert alert-dismissable">
                <div class="d-flex align-items-center show-child-on-hover">
                  <span class="d-flex flex-column flex-1">
                    <span class="name d-flex align-items-center">Administrator <span
                        class="badge bg-success fw-n ms-1">UPDATE</span></span>
                    <span class="msg-a fs-sm">
                      System updated to version <strong>5.0</strong> <a href="buildnotes.html">(build notes)</a>
                    </span>
                    <span class="fs-nano text-muted mt-1">5 mins ago</span>
                  </span>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center show-child-on-hover">
                  <div class="d-flex flex-column flex-1">
                    <span class="name">
                      Adison Lee <span class="fw-300 d-inline">replied to your video <a href="#" class="fw-400">
                          Cancer Drug</a> </span>
                    </span>
                    <span class="msg-a fs-sm mt-2">Bring to the table win-win survival strategies to ensure proactive
                      domination. At the end of the day...</span>
                    <span class="fs-nano text-muted mt-1">10 minutes ago</span>
                  </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center show-child-on-hover">
                  <div class="d-flex flex-column flex-1">
                    <span class="name">
                      Troy Norman'<span class="fw-300">s new connections</span>
                    </span>
                    <div class="fs-sm d-flex align-items-center mt-2">
                      <span class="profile-image-md ms-1 rounded-circle d-inline-block"
                        style="background-image:url('/assets/img/demo/avatars/avatar-a.png'); background-size: cover;"></span>
                      <span class="profile-image-md ms-1 rounded-circle d-inline-block"
                        style="background-image:url('/assets/img/demo/avatars/avatar-b.png'); background-size: cover;"></span>
                      <span class="profile-image-md ms-1 rounded-circle d-inline-block"
                        style="background-image:url('/assets/img/demo/avatars/avatar-c.png'); background-size: cover;"></span>
                      <span class="profile-image-md ms-1 rounded-circle d-inline-block"
                        style="background-image:url('/assets/img/demo/avatars/avatar-e.png'); background-size: cover;"></span>
                      <div data-hasmore="+3" class="rounded-circle profile-image-md ms-1">
                        <span class="profile-image-md ms-1 rounded-circle d-inline-block"
                          style="background-image:url('/assets/img/demo/avatars/avatar-h.png'); background-size: cover;"></span>
                      </div>
                    </div>
                    <span class="fs-nano text-muted mt-1">55 minutes ago</span>
                  </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center show-child-on-hover">
                  <div class="d-flex flex-column flex-1">
                    <span class="name">Dr John Cook <span class="fw-300">sent a <span class="text-danger">new
                          signal</span></span></span>
                    <span class="msg-a fs-sm mt-2">Nanotechnology immersion along the information highway will close
                      the loop on focusing solely on the bottom line.</span>
                    <span class="fs-nano text-muted mt-1">10 minutes ago</span>
                  </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center show-child-on-hover">
                  <div class="d-flex flex-column flex-1">
                    <span class="name">Lab Images <span class="fw-300">were updated!</span></span>
                    <div class="fs-sm d-flex align-items-center mt-1">
                      <a href="#" class="ms-1 mt-1" title="Cell A-0012">
                        <span class="d-block img-share"
                          style="background-image:url('/assets/img/thumbs/pic-7.png'); background-size: cover;"></span>
                      </a>
                      <a href="#" class="ms-1 mt-1" title="Patient A-473 saliva">
                        <span class="d-block img-share"
                          style="background-image:url('/assets/img/thumbs/pic-8.png'); background-size: cover;"></span>
                      </a>
                      <a href="#" class="ms-1 mt-1" title="Patient A-473 blood cells">
                        <span class="d-block img-share"
                          style="background-image:url('/assets/img/thumbs/pic-11.png'); background-size: cover;"></span>
                      </a>
                      <a href="#" class="ms-1 mt-1" title="Patient A-473 Membrane O.C">
                        <span class="d-block img-share"
                          style="background-image:url('/assets/img/thumbs/pic-12.png'); background-size: cover;"></span>
                      </a>
                    </div>
                    <span class="fs-nano text-muted mt-1">55 minutes ago</span>
                  </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
              <li class="alert alert-dismissable">
                <div class="d-flex align-items-center show-child-on-hover">
                  <div class="d-flex flex-column flex-1 w-100">
                    <div class="name mb-2"> Lisa Lamar<span class="fw-300"> updated project</span>
                    </div>
                    <div class="row fs-b fw-300">
                      <div class="col text-start"> Progress </div>
                      <div class="col text-end fw-500"> 45% </div>
                    </div>
                    <div class="progress progress-sm d-flex mt-1">
                      <span class="progress-bar bg-primary progress-bar-striped" role="progressbar" style="width: 45%"
                        aria-valuenow="45" aria-valuemin="0" aria-valuemax="100"></span>
                    </div>
                    <span class="fs-nano text-muted mt-1">2 hrs ago</span>
                  </div>
                </div>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
              </li>
            </ul>
            <div class="notification-empty-msg">
              <svg class="sa-icon sa-icon-5x sa-icon-primary">
                <use href="/assets/icons/sprite.svg#smile"></use>
              </svg>
              <span>
                You are all set!
              </span>
            </div>
          </div>

        </div>
        <!-- Events -->
        <div class="tab-pane fade" id="tab-events" role="tabpanel">
          <div class="d-flex flex-column h-100">
            <div class="h-auto">
              <table class="table-calendar m-0 w-100 h-100 border-0">
                <thead>
                  <tr>
                    <th colspan="7" class="pt-3 pb-2 px-3 text-center">
                      <div class="js-get-date h6 fw-600 mb-2">Fake Day, October 15th, 2090</div>
                    </th>
                  </tr>
                  <tr class="text-center">
                    <th>Sun</th>
                    <th>Mon</th>
                    <th>Tue</th>
                    <th>Wed</th>
                    <th>Thu</th>
                    <th>Fri</th>
                    <th>Sat</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td class="text-muted bg-faded">30</td>
                    <td>1</td>
                    <td>2</td>
                    <td>3</td>
                    <td>4</td>
                    <td>5</td>
                    <td>
                      <svg class="sa-icon sa-icon-warning m-1 position-absolute pos-left pos-top"
                        style="--sa-icon-size: 0.85rem; --sa-fill-opacity: 0.5;">
                        <use href="/assets/icons/sprite.svg#star"></use>
                      </svg>
                      6
                    </td>
                  </tr>
                  <tr>
                    <td>7</td>
                    <td>8</td>
                    <td>9</td>
                    <td class="bg-primary-600 text-white pattern-0">10</td>
                    <td>11</td>
                    <td>12</td>
                    <td>13</td>
                  </tr>
                  <tr>
                    <td>14</td>
                    <td>15</td>
                    <td>16</td>
                    <td>17</td>
                    <td>18</td>
                    <td>19</td>
                    <td>20</td>
                  </tr>
                  <tr>
                    <td>21</td>
                    <td>
                      <svg class="sa-icon sa-icon-info m-1 position-absolute pos-left pos-top"
                        style="--sa-icon-size: 0.85rem; --sa-fill-opacity: 0.5;">
                        <use href="/assets/icons/sprite.svg#shield"></use>
                      </svg>
                      22
                    </td>
                    <td>23</td>
                    <td>24</td>
                    <td>25</td>
                    <td>26</td>
                    <td>27</td>
                  </tr>
                  <tr>
                    <td>28</td>
                    <td>29</td>
                    <td>30</td>
                    <td>31</td>
                    <td class="text-muted bg-faded">1</td>
                    <td class="text-muted bg-faded">2</td>
                    <td class="text-muted bg-faded">3</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="flex-1 custom-scroll shadow-inset-3">
              <div class="p-2">
                <div class="d-flex align-items-center text-left mb-3">
                  <div class="width-5 text-primary align-self-start table-calendar-appointment-date fw-300 text-center">
                    15
                  </div>
                  <div class="flex-1">
                    <div class="d-flex flex-column">
                      <span class="l-h-n fs-md fw-500">
                        October 2020
                      </span>
                      <span class="l-h-n fs-nano fw-400 text-secondary">
                        Monday
                      </span>
                    </div>
                    <div class="d-flex flex-column gap-2 mt-2">
                      <div>
                        <strong>2:30PM</strong> - Doctor's appointment
                      </div>
                      <div>
                        <strong>3:30PM</strong> - Report overview
                      </div>
                      <div>
                        <strong>4:30PM</strong> - Meeting with Donnah V.
                      </div>
                      <div>
                        <strong>5:30PM</strong> - Late Lunch
                      </div>
                      <div>
                        <strong>6:30PM</strong> - Report Compression
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div class="py-2 px-3 d-block rounded-bottom text-end border-light border-bottom-0 border-end-0 border-start-0">
        <a href="#" class="fs-xs fw-500 ms-auto">view all notifications</a>
      </div>
    </div>

    <!-- Profile -->
    <button type="button" data-bs-toggle="dropdown" :title="authStore.getUser?.Email"
      class="btn-system bg-transparent d-flex flex-shrink-0 align-items-center justify-content-center"
      aria-label="Open Profile Dropdown">
      <img src="/assets/img/demo/avatars/avatar-admin.png" class="profile-image profile-image-md rounded-circle"
        :alt="authStore.getUser?.FullName">
    </button>

    <!-- Profile dropdown -->
    <div class="dropdown-menu dropdown-menu-animated">
      <div class="notification-header rounded-top mb-2">
        <div class="d-flex flex-row align-items-center mt-1 mb-1 color-white">
          <span class="status status-success d-inline-block me-2">
            <img src="/assets/img/demo/avatars/avatar-admin.png" class="profile-image rounded-circle"
              :alt="authStore.getUser?.FullName">
          </span>
          <div class="info-card-text">
            <div class="fs-lg text-truncate text-truncate-lg">{{ authStore.getUser?.FullName }}</div>
            <span class="text-truncate text-truncate-md opacity-80 fs-sm">{{ authStore.getUser?.Email }}</span>
          </div>
        </div>
      </div>

      <div class="dropdown-divider m-0"></div>

      <!-- <a href="#" class="dropdown-item" data-action="app-reset" role="button">
        <span data-i18n="drpdwn.reset_layout">Reset Layout</span>
      </a> -->
      <a href="#" class="dropdown-item" @click="layoutStore.toggleSettingsDrawer()" role="button">
        <span data-i18n="drpdwn.settings">Ajustes</span>
      </a>


      <div class="dropdown-divider m-0"></div>

      <a href="#" class="dropdown-item d-flex justify-content-between align-items-center" @click="toggleFullscreen()"
        aria-pressed="false" role="button">
        <span data-i18n="drpdwn.fullscreen">Pantalla completa</span>
        <b class="text-muted fs-nano px-2 rounded font-monospace align-self-center border">F11</b>
      </a>
      <a href="#" class="dropdown-item d-flex justify-content-between align-items-center" @click="printPage()"
        role="button">
        <span data-i18n="drpdwn.print">Imprimir</span>
        <span class="text-muted fs-nano px-2 rounded font-monospace align-self-center border">
          <svg width="15" height="15">
            <path
              d="M4.505 4.496h2M5.505 5.496v5M8.216 4.496l.055 5.993M10 7.5c.333.333.5.667.5 1v2M12.326 4.5v5.996M8.384 4.496c1.674 0 2.116 0 2.116 1.5s-.442 1.5-2.116 1.5M3.205 9.303c-.09.448-.277 1.21-1.241 1.203C1 10.5.5 9.513.5 8V7c0-1.57.5-2.5 1.464-2.494.964.006 1.134.598 1.24 1.342M12.553 10.5h1.953"
              stroke-width="1.2" stroke="currentColor" fill="none" stroke-linecap="square"></path>
          </svg>
          + P
        </span>
      </a>


      <div class="dropdown-divider m-0"></div>

      <a class="dropdown-item py-3 fw-500 d-flex justify-content-between" href="javascript:void(0)" @click="logout()">
        <span class="text-danger" data-i18n="drpdwn.page-logout">Cerrar sesion</span>
        <span class="d-block text-truncate text-truncate-sm">{{ authStore.getUser?.UserName }}</span>
      </a>
    </div>
  </header>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/modules/auth/stores/auth.store";

import { useRouter } from "vue-router";


const authStore = useAuthStore();
const router = useRouter();

import { useThemeStore } from '@/stores/themeStore';
import { useLayoutStore } from '@/stores/layoutStore';
import { useApp } from '@/composables/useApp';

const themeStore = useThemeStore();
const layoutStore = useLayoutStore();
const { toggleFullscreen, printPage } = useApp();



const logout = async () => {
  router.push({ name: 'login' });
  await authStore.logout();
}
</script>

<style scoped></style>
