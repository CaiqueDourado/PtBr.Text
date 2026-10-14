using PtBr.Text.Documents;

namespace PtBr.Text.Tests;

public class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    [InlineData(" 529 982 247 25 ")]
    public void Valid(string cpf) => Assert.True(Cpf.IsValid(cpf));

    [Theory]
    [InlineData("529.982.247-24")] // wrong check digit
    [InlineData("111.111.111-11")] // passes arithmetic, not a real CPF
    [InlineData("000.000.000-00")]
    [InlineData("5299822472")]     // 10 digits
    [InlineData("")]
    public void Invalid(string cpf) => Assert.False(Cpf.IsValid(cpf));

    [Fact]
    public void Null_is_invalid() => Assert.False(Cpf.IsValid(null));

    [Fact]
    public void Formats() => Assert.Equal("529.982.247-25", Cpf.Format("52998224725"));

    [Fact]
    public void Computes_check_digits() => Assert.Equal("25", Cpf.CheckDigits("529982247"));

    [Fact]
    public void Generated_cpfs_are_valid()
    {
        var random = new Random(42);
        for (var i = 0; i < 200; i++)
        {
            Assert.True(Cpf.IsValid(Cpf.Generate(random)));
        }

        Assert.Matches("^\\d{3}\\.\\d{3}\\.\\d{3}-\\d{2}$", Cpf.Generate(random, formatted: true));
    }
}

public class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    [InlineData("12.ABC.345/01DE-35")] // example published by the Receita Federal
    [InlineData("12abc34501de35")]     // lower case is accepted
    public void Valid(string cnpj) => Assert.True(Cnpj.IsValid(cnpj));

    [Theory]
    [InlineData("11.222.333/0001-82")]
    [InlineData("12.ABC.345/01DE-36")]
    [InlineData("00.000.000/0000-00")]
    [InlineData("12.ABC.345/01DE-3X")] // check digits must be numeric
    [InlineData("11.222.333/0001")]
    [InlineData("11.222.333/0001-81!")]
    public void Invalid(string cnpj) => Assert.False(Cnpj.IsValid(cnpj));

    [Fact]
    public void Formats_alphanumeric() => Assert.Equal("12.ABC.345/01DE-35", Cnpj.Format("12abc34501de35"));

    [Fact]
    public void Detects_alphanumeric()
    {
        Assert.True(Cnpj.IsAlphanumeric("12.ABC.345/01DE-35"));
        Assert.False(Cnpj.IsAlphanumeric("11.222.333/0001-81"));
    }

    [Fact]
    public void Root_and_head_office()
    {
        Assert.Equal("11222333", Cnpj.Root("11.222.333/0001-81"));
        Assert.True(Cnpj.IsHeadOffice("11.222.333/0001-81"));
        Assert.False(Cnpj.IsHeadOffice("12.ABC.345/01DE-35"));
    }

    [Fact]
    public void Generated_cnpjs_are_valid()
    {
        var random = new Random(7);
        for (var i = 0; i < 200; i++)
        {
            Assert.True(Cnpj.IsValid(Cnpj.Generate(random)));
            Assert.True(Cnpj.IsValid(Cnpj.Generate(random, alphanumeric: true)));
        }
    }

    [Fact]
    public void Check_digits_reject_symbols() =>
        Assert.Throws<ArgumentException>(() => Cnpj.CheckDigits("12ABC34501D!"));
}

public class CepTests
{
    [Theory]
    [InlineData("01310-100", "SP")]
    [InlineData("20040-020", "RJ")]
    [InlineData("70040-010", "DF")]
    [InlineData("40020000", "BA")]
    [InlineData("69900-000", "AC")]
    [InlineData("90010-000", "RS")]
    public void State_by_range(string cep, string uf) => Assert.Equal(uf, Cep.GetState(cep));

    [Theory]
    [InlineData("00000-000")]
    [InlineData("1234-567")]
    [InlineData("")]
    public void Invalid(string cep) => Assert.False(Cep.IsValid(cep));

    [Fact]
    public void Formats() => Assert.Equal("01310-100", Cep.Format("01310100"));
}

public class PhoneTests
{
    [Theory]
    [InlineData("(11) 98765-4321", PhoneKind.Mobile, "(11) 98765-4321", "+5511987654321")]
    [InlineData("11987654321", PhoneKind.Mobile, "(11) 98765-4321", "+5511987654321")]
    [InlineData("+55 11 98765 4321", PhoneKind.Mobile, "(11) 98765-4321", "+5511987654321")]
    [InlineData("011 3456-7890", PhoneKind.Landline, "(11) 3456-7890", "+551134567890")]
    [InlineData("0 21 11 3456-7890", PhoneKind.Landline, "(11) 3456-7890", "+551134567890")] // carrier code
    [InlineData("(55) 3222-1234", PhoneKind.Landline, "(55) 3222-1234", "+555532221234")]   // DDD 55 is Santa Maria/RS
    [InlineData("+55 55 99999-1234", PhoneKind.Mobile, "(55) 99999-1234", "+5555999991234")]
    public void Parses_geographic_numbers(string input, PhoneKind kind, string formatted, string e164)
    {
        var phone = Phone.Parse(input);

        Assert.NotNull(phone);
        Assert.Equal(kind, phone!.Kind);
        Assert.Equal(formatted, phone.Formatted);
        Assert.Equal(e164, phone.E164);
        Assert.Equal("tel:" + e164, phone.TelUri);
    }

    [Theory]
    [InlineData("0800 123 4567", PhoneKind.TollFree, "0800 123 4567")]
    [InlineData("0800-1234567", PhoneKind.TollFree, "0800 123 4567")]
    [InlineData("0300 789 1234", PhoneKind.NonGeographic, "0300 789 1234")]
    [InlineData("4004-0001", PhoneKind.SingleNumber, "4004-0001")]
    [InlineData("3003 1234", PhoneKind.SingleNumber, "3003-1234")]
    public void Parses_special_numbers(string input, PhoneKind kind, string formatted)
    {
        var phone = Phone.Parse(input);

        Assert.NotNull(phone);
        Assert.Equal(kind, phone!.Kind);
        Assert.Equal(formatted, phone.Formatted);
        Assert.Null(phone.E164);
    }

    [Theory]
    [InlineData("(20) 98765-4321")] // DDD 20 does not exist
    [InlineData("(11) 8765-4321")]  // 8 digits starting with 8 is not a landline
    [InlineData("(11) 18765-4321")] // 9 digits not starting with 9
    [InlineData("3456-7890")]       // no DDD
    [InlineData("123")]
    [InlineData("")]
    public void Rejects_invalid_numbers(string input) => Assert.Null(Phone.Parse(input));

    [Fact]
    public void There_are_67_area_codes() => Assert.Equal(67, Phone.AreaCodes.Count);
}
