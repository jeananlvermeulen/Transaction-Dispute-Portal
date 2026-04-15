using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Validators;
using FluentAssertions;

namespace Capitec.Dispute.Application.Tests.Validators;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    private static RegisterUserRequestDto ValidRequest() => new()
    {
        Email = "user@example.com",
        Password = "Password1!",
        ConfirmPassword = "Password1!",
        FirstName = "John",
        LastName = "Doe",
        PhoneNumber = "+27821234567"
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
    [InlineData("@nodomain.com")]
    public void Invalid_email_fails_validation(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Password_shorter_than_8_chars_fails()
    {
        var request = ValidRequest();
        request.Password = "Ab1!";
        request.ConfirmPassword = "Ab1!";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Password_without_uppercase_fails()
    {
        var request = ValidRequest();
        request.Password = "password1!";
        request.ConfirmPassword = "password1!";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Password_without_digit_fails()
    {
        var request = ValidRequest();
        request.Password = "Password!!";
        request.ConfirmPassword = "Password!!";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Password_without_special_char_fails()
    {
        var request = ValidRequest();
        request.Password = "Password123";
        request.ConfirmPassword = "Password123";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Mismatched_confirm_password_fails()
    {
        var request = ValidRequest();
        request.ConfirmPassword = "DifferentPass1!";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ThisFirstNameIsWayTooLongForValidationAndExceedsFiftyCharacters")]
    public void Invalid_first_name_fails(string firstName)
    {
        var request = ValidRequest();
        request.FirstName = firstName;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("X")]
    [InlineData("ThisLastNameIsWayTooLongForValidationAndExceedsFiftyCharactersMax")]
    public void Invalid_last_name_fails(string lastName)
    {
        var request = ValidRequest();
        request.LastName = lastName;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0821234567")]
    public void Invalid_phone_number_fails(string phone)
    {
        var request = ValidRequest();
        request.PhoneNumber = phone;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Theory]
    [InlineData("+27821234567")]
    [InlineData("+12025551234")]
    [InlineData("1234567890")]
    public void Valid_phone_numbers_pass(string phone)
    {
        var request = ValidRequest();
        request.PhoneNumber = phone;

        var result = _validator.Validate(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "PhoneNumber");
    }
}
