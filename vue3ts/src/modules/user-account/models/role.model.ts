export class Role {
    Id: number = 0;
    NameRol: string = '';
    Description: string = '';
    State: boolean = true;
}

export class RoleFormAssignment {
    RolId: number = 0;
    FormIds: number[] = [];
}
