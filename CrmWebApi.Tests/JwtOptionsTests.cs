using CrmWebApi.Options;

namespace CrmWebApi.Tests;

public class JwtOptionsTests
{
	[Theory]
	[InlineData("")]
	[InlineData("short")]
	[InlineData("REPLACE_WITH_MIN_32_CHAR_SECRET_KEY_HERE!!")]
	public void HasValidSecret_RejectsMissingWeakAndPlaceholderSecrets(string secret)
	{
		Assert.False(JwtOptions.HasValidSecret(new JwtOptions { Secret = secret }));
	}
}
