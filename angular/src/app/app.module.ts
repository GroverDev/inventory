import { importProvidersFrom, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

// search module
import { NgPipesModule } from 'ngx-pipes';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

import { LayoutsModule } from "./layouts/layouts.module";

// Auth
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { environment } from '../environments/environment';
import { apiInterceptor } from './core/helpers/api.interceptor';
import { DialogComponent } from './shared/dialog/dialog.component';

// Language
import { TRANSLATE_HTTP_LOADER_CONFIG, TranslateHttpLoader } from '@ngx-translate/http-loader';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';

// Store
import { rootReducer } from './store';
import { StoreModule } from '@ngrx/store';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';

export function createTranslateLoader(http: HttpClient): any {
  return new TranslateHttpLoader();
}

@NgModule({
  declarations: [
    AppComponent
  ],
  bootstrap: [AppComponent], imports: [TranslateModule.forRoot({
    defaultLanguage: 'es',
    loader: {
      provide: TranslateLoader,
      useFactory: (createTranslateLoader),
      deps: [HttpClient]
    }
  }),
    BrowserAnimationsModule,
    BrowserModule,
    AppRoutingModule,
    LayoutsModule,
    DialogComponent,
  StoreModule.forRoot(rootReducer),
  StoreDevtoolsModule.instrument({
    maxAge: 25, // Retains last 25 states
    logOnly: environment.production, // Restrict extension to log-only mode
  }),
    NgPipesModule], providers: [
      importProvidersFrom(
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useFactory: createTranslateLoader,
            deps: [HttpClient],
          },
          defaultLanguage: 'es',
        }),
      ),
      {
        provide: TRANSLATE_HTTP_LOADER_CONFIG,
        useValue: {
          prefix: 'assets/i18n/',
          suffix: '.json'
        },
      },
      provideHttpClient(withInterceptors([apiInterceptor])),
    ]
})
export class AppModule { }
