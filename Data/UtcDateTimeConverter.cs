using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrmWebApi.Data;

public abstract class UtcDateTimeConverter()
	: ValueConverter<DateTime, DateTime>(
		v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
		v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
	);

public abstract class UtcDateTimeOffsetConverter()
	: ValueConverter<DateTimeOffset, DateTimeOffset>(v => v.ToUniversalTime(), v => v);
