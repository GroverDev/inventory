using System;
using FluentValidation;

namespace Seguridad.Domain.Entities.requests;

public class UserRequest
{
        public string Email { get; set; }="";
        public string Password { get; set; }="";
        public string FullName { get; set; }="";
}
public class UserRequestValidator : AbstractValidator<UserRequest>
{
    public UserRequestValidator()
    {
        RuleFor(user => user.Email)
            .Cascade(CascadeMode.Stop)
            .Length(3, 70).WithMessage("El correo electrónico debe tener entre 3 y 70 caracteres")
            .When(user => user.FullName.Length== 0)
            .WithMessage("El correo electrónico es un dato obligatorio y minimamente debe tener 3 y maximo 70 caracteres.");

        RuleFor(user => user.FullName)
            .Cascade(CascadeMode.Stop)
            .Length(3, 70).WithMessage("El nombre del usuario debe tener entre 3 y 70 caracteres")
            .When(user => user.Email.Length== 0)
            .WithMessage("El  nombre del usuario es un dato obligatorioy minimamente debe tener 3 y maximo 70 caracteres.");


    }
}