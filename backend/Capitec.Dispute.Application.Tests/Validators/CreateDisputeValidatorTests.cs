using Capitec.Dispute.Application.DTOs;
using Capitec.Dispute.Application.Validators;
using FluentAssertions;

namespace Capitec.Dispute.Application.Tests.Validators;

public class CreateDisputeValidatorTests
{
    private readonly CreateDisputeValidator _validator = new();

    private static CreateDisputeRequestDto ValidRequest() => new()
    {
        TransactionId = Guid.NewGuid(),
        Reason = "Unauthorised",
        Summary = "This transaction was not authorised by me and I did not make this purchase."
    };

    [Fact]
    public void Valid_request_passes_validation()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_transaction_id_fails()
    {
        var request = ValidRequest();
        request.TransactionId = Guid.Empty;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TransactionId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("InvalidReason")]
    [InlineData("unauthorised")]  // case-sensitive
    [InlineData("UNAUTHORISED")]
    public void Invalid_reason_fails(string reason)
    {
        var request = ValidRequest();
        request.Reason = reason;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Theory]
    [InlineData("Unauthorised")]
    [InlineData("IncorrectAmount")]
    [InlineData("Other")]
    public void Valid_reasons_pass(string reason)
    {
        var request = ValidRequest();
        request.Reason = reason;

        var result = _validator.Validate(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Empty_summary_fails()
    {
        var request = ValidRequest();
        request.Summary = "";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Summary");
    }

    [Fact]
    public void Summary_shorter_than_10_chars_fails()
    {
        var request = ValidRequest();
        request.Summary = "Too short";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Summary");
    }

    [Fact]
    public void Summary_longer_than_500_chars_fails()
    {
        var request = ValidRequest();
        request.Summary = new string('A', 501);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Summary");
    }

    [Fact]
    public void Summary_of_exactly_10_chars_passes()
    {
        var request = ValidRequest();
        request.Summary = "1234567890";

        var result = _validator.Validate(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "Summary");
    }

    [Fact]
    public void Summary_of_exactly_500_chars_passes()
    {
        var request = ValidRequest();
        request.Summary = new string('A', 500);

        var result = _validator.Validate(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "Summary");
    }
}
