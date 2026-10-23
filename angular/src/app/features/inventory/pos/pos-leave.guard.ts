import { CanDeactivateFn } from '@angular/router';

import { PosComponent } from './pos.component';

/** Pregunta antes de salir del punto de venta si hay una venta armada. */
export const posLeaveGuard: CanDeactivateFn<PosComponent> = (component, _route, _state, nextState) =>
  component.canLeave(nextState?.url ?? '');
