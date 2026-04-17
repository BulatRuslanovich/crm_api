using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrmWebApi.Data;

public class UtcDateTimeConverter()
	: ValueConverter<DateTime, DateTime>(
		v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
		v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
	);

public class UtcDateTimeOffsetConverter()
	: ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);
