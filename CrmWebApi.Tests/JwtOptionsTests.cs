using CrmWebApi.Options;

namespace CrmWebApi.Tests;

public class JwtOptionsTests
{
	[Theory]
	[InlineData("")]
	[InlineData("short")]
	[InlineData("REPLACE_WITH_MIN_32_CHAR_SECRET_KEY_HERE!!")]
	[InlineData("CHANGE_ME_CHANGE_ME_CHANGE_ME_CHANGE_ME")]
	public void HasValidSecret_RejectsMissingWeakAndPlaceholderSecrets(string secret)
	{
		Assert.False(JwtOptions.HasValidSecret(new JwtOptions { Secret = secret }));
	}

	[Fact]
	public void HasValidSecret_AcceptsLongNonPlaceholderSecret()
	{
		Assert.True(
			JwtOptions.HasValidSecret(
				new JwtOptions { Secret = "a-realistic-local-test-secret-32-chars" }
			)
		);
	}
}
