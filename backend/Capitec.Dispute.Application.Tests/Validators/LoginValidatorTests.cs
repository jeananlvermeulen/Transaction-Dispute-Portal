using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Validators;
using FluentAssertions;

namespace Capitec.Dispute.Application.Tests.Validators;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    private static LoginRequestDto ValidRequest() => new()
    {
        Email = "user@example.com",
        Password = "AnyPassword1!"
    };

    [Fact]
    public void Valid_request_passes_validation()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    public void Invalid_email_fails_validation(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Empty_password_fails_validation()
    {
        var request = ValidRequest();
        request.Password = "";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Optional_mfa_code_does_not_affect_validation()
    {
        var request = ValidRequest();
        request.MfaCode = null;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
