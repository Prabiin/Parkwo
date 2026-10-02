using Microsoft.Extensions.Options;
using ParkingApp.Application.Common;
using ParkingApp.Application.Common.Helpers;
using ParkingApp.Application.Configuration;

namespace ParkingApp.Tests;

public class ParkingPassServiceTests
{
    private const string Key = "unit-test-signing-key-0123456789abcdef";
    private const string OtherKey = "a-different-key-0123456789abcdefgh";

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly Guid BookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FacilityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Nonce = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ParkingPassService Service(string key = Key, int earlyGrace = 15, int lateGrace = 30)
        => new(Options.Create(new PassSettings
        {
            SigningKey = key,
            EarlyEntryGraceMinutes = earlyGrace,
            LateEntryGraceMinutes = lateGrace
        }));

    private static string Issue(ParkingPassService service, Guid? nonce = null, Guid? facilityId = null)
        => service.Issue(
            BookingId,
            facilityId ?? FacilityId,
            nonce ?? Nonce,
            Now.AddMinutes(-15),
            Now.AddHours(2));

    [Fact]
    public void A_Freshly_Issued_Pass_Verifies()
    {
        var service = Service();
        var claims = service.Verify(Issue(service), Now);

        Assert.NotNull(claims);
        Assert.Equal(BookingId, claims!.BookingId);
        Assert.Equal(FacilityId, claims.FacilityId);
        Assert.Equal(Nonce, claims.PassNonce);
    }

    [Fact]
    public void The_Pass_Encodes_The_Booking_And_Facility_It_Belongs_To()
    {
        // Staff phones can validate offline, so the claims have to travel with
        // the token rather than requiring a database round trip.
        var service = Service();
        var claims = service.Verify(Issue(service), Now);

        Assert.Equal(BookingId, claims!.BookingId);
        Assert.Equal(FacilityId, claims.FacilityId);
    }

    [Fact]
    public void The_Pass_Does_Not_Leak_The_Riders_Name_Or_Vehicle_Number()
    {
        // A QR is a screenshot magnet; it should carry no PII to leak.
        var service = Service();
        var token = Issue(service);

        Assert.DoesNotContain("vehicle", token, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user", token, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_Pass_Signed_With_Another_Key_Is_Rejected()
    {
        var token = Issue(Service());

        Assert.Null(Service(OtherKey).Verify(token, Now));
    }

    [Fact]
    public void A_Tampered_Payload_Is_Rejected()
    {
        var service = Service();
        var parts = Issue(service).Split('.');

        // Swap in another facility without re-signing.
        var forged = Service().Issue(BookingId, Guid.NewGuid(), Nonce, Now.AddMinutes(-15), Now.AddHours(2));
        var forgedPayload = forged.Split('.')[1];

        var tampered = $"{parts[0]}.{forgedPayload}.{parts[2]}";

        Assert.Null(service.Verify(tampered, Now));
    }

    [Fact]
    public void A_Truncated_Token_Is_Rejected()
    {
        var service = Service();
        var token = Issue(service);

        Assert.Null(service.Verify(token[..20], Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-pass")]
    [InlineData("PKP1.only-two-parts")]
    [InlineData("PKP1.a.b.c")]
    [InlineData("XXXX.aaaa.bbbb")]
    public void Malformed_Input_Is_Rejected_Without_Throwing(string? token)
    {
        Assert.Null(Service().Verify(token, Now));
    }

    [Fact]
    public void An_Overlong_Token_Is_Rejected_Without_Being_Processed()
    {
        Assert.Null(Service().Verify(new string('a', 5000), Now));
    }

    [Fact]
    public void An_Expired_Pass_Fails_Verification()
    {
        var service = Service();
        var token = Issue(service);

        Assert.Null(service.Verify(token, Now.AddHours(3)));
    }

    [Fact]
    public void An_Expired_Pass_Is_Distinguishable_From_A_Forged_One()
    {
        // "Expired, rebook" and "forged, call support" are different conversations
        // with the rider, so the gate needs to tell them apart.
        var service = Service();
        var token = Issue(service);
        var later = Now.AddHours(3);

        Assert.Null(service.Verify(token, later));
        Assert.True(service.IsExpired(token, later));
        Assert.False(service.IsExpired(token, Now));
    }

    [Fact]
    public void A_Forged_Pass_Is_Not_Reported_As_Merely_Expired()
    {
        Assert.False(Service().IsExpired(Issue(Service(OtherKey)), Now));
    }

    [Fact]
    public void VerifySignatureOnly_Ignores_The_Time_Window()
    {
        // The exit path must accept the rider who is overdue — that is exactly
        // who needs to be let out of the lot.
        var service = Service();
        var token = Issue(service);

        Assert.Null(service.Verify(token, Now.AddDays(1)));
        Assert.NotNull(service.VerifySignatureOnly(token));
    }

    [Fact]
    public void VerifySignatureOnly_Still_Rejects_A_Forged_Pass()
    {
        var token = Issue(Service());

        Assert.Null(Service(OtherKey).VerifySignatureOnly(token));
    }

    [Fact]
    public void Verify_Rejects_A_Pass_Used_Before_Its_Window_Opens()
    {
        // The lower bound matters: a pass for tomorrow's slot must not admit a
        // rider today, even though its signature is perfectly valid.
        var service = Service();
        var token = service.Issue(
            BookingId,
            FacilityId,
            Nonce,
            Now.AddHours(2),
            Now.AddHours(4));

        Assert.Null(service.Verify(token, Now));
        Assert.NotNull(service.Verify(token, Now.AddHours(3)));
    }

    [Fact]
    public void Rotating_The_Nonce_Invalidates_The_Previous_Pass()
    {
        // The leak response: same booking, new secret, old screenshot dead.
        var service = Service();
        var oldToken = Issue(service);
        var newToken = Issue(service, nonce: Guid.NewGuid());

        Assert.Equal(BookingId, service.Verify(oldToken, Now)!.BookingId);
        Assert.NotEqual(oldToken, newToken);
        Assert.Equal(Nonce, service.Verify(oldToken, Now)!.PassNonce);
        Assert.NotEqual(Nonce, service.Verify(newToken, Now)!.PassNonce);
    }

    [Fact]
    public void Validity_Window_Applies_The_Entry_Graces()
    {
        var service = Service(earlyGrace: 15, lateGrace: 30);
        var startsAt = Now.AddHours(1);
        var endsAt = startsAt.AddHours(2);

        var validity = service.ValidityFor(startsAt, endsAt);

        Assert.Equal(startsAt.AddMinutes(-15), validity.From);
        Assert.Equal(endsAt.AddMinutes(30), validity.Until);
    }

    [Fact]
    public void Validity_Clamps_Negative_Grace_Configuration()
    {
        var service = Service(earlyGrace: -60, lateGrace: -60);
        var startsAt = Now.AddHours(1);
        var endsAt = startsAt.AddHours(2);

        var validity = service.ValidityFor(startsAt, endsAt);

        Assert.Equal(startsAt, validity.From);
        Assert.Equal(endsAt, validity.Until);
    }

    [Fact]
    public void A_Pass_Is_Valid_Throughout_Its_Issued_Window()
    {
        var service = Service();
        var token = Issue(service);

        Assert.NotNull(service.Verify(token, Now));
        Assert.NotNull(service.Verify(token, Now.AddHours(1)));
    }

    [Fact]
    public void An_Unconfigured_Environment_Reports_Itself_As_Such()
    {
        Assert.False(Service(string.Empty).IsConfigured);
        Assert.False(Service("   ").IsConfigured);
        Assert.True(Service().IsConfigured);
    }

    [Fact]
    public void An_Unconfigured_Environment_Refuses_To_Issue_A_Pass()
    {
        // Failing closed matters more than degrading quietly: a token signed with
        // an empty key would be forgeable by anyone who guessed it was empty.
        var service = Service(string.Empty);

        var error = Assert.Throws<InvalidOperationException>(() =>
            service.Issue(BookingId, FacilityId, Nonce, Now.AddHours(-1), Now.AddHours(1)));

        Assert.Contains("Pass__SigningKey", error.Message);
    }

    [Fact]
    public void An_Unconfigured_Environment_Verifies_Nothing()
    {
        var service = Service(string.Empty);

        Assert.Null(service.Verify(Issue(Service()), Now));
        Assert.Null(service.VerifySignatureOnly(Issue(Service())));
        Assert.False(service.IsExpired(Issue(Service()), Now));
    }

    [Fact]
    public void The_Not_Configured_Message_Names_The_Missing_Variable()
    {
        // A support ticket should be actionable without reading the source.
        Assert.Contains("Pass__SigningKey", ParkingPassService.NotConfiguredMessage);
    }

    [Fact]
    public void A_Pass_From_A_Configured_Environment_Is_Refused_By_An_Unconfigured_One()
    {
        Assert.Null(Service(string.Empty).Verify(Issue(Service()), Now));
    }

    [Fact]
    public void The_Issued_Pass_Verifies_Inside_The_Window_It_Advertises()
    {
        // The rider is told "valid 09:45-12:30" on the QR screen, so those exact
        // instants must be the boundaries the gate enforces.
        var service = Service(earlyGrace: 15, lateGrace: 30);
        var startsAt = Now.AddHours(1);
        var endsAt = startsAt.AddHours(2);

        var validity = service.ValidityFor(startsAt, endsAt);
        var token = service.Issue(BookingId, FacilityId, Nonce, validity.From, validity.Until);

        Assert.Null(service.Verify(token, validity.From.AddSeconds(-1)));
        Assert.NotNull(service.Verify(token, validity.From));
        Assert.NotNull(service.Verify(token, validity.Until));
        Assert.Null(service.Verify(token, validity.Until.AddSeconds(1)));
    }
}
