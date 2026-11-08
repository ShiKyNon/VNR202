using HanhTrinh.Web.Game;

namespace HanhTrinh.Tests;

public class NameFilterTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901234567")]
    public void Rejects_Empty_And_Over16(string n) => Assert.False(NameFilter.Validate(n).Ok);

    [Fact]
    public void Trims() => Assert.Equal("Lan Anh", NameFilter.Validate("  Lan   Anh ").Name);

    [Theory]
    [InlineData("Nguyễn Văn An")]
    [InlineData("Đức Long")]
    [InlineData("Lớp SE1801")]
    [InlineData("Cúc")]
    public void Accepts_VietnameseNames(string n) => Assert.True(NameFilter.Validate(n).Ok);

    [Theory]
    [InlineData("ĐM")]
    [InlineData("Địt")]
    [InlineData("vcl")]
    [InlineData("đitme")]
    [InlineData("FUCK you")]
    [InlineData("Lồn")]
    public void Rejects_Profanity_AccentAndCaseInsensitive(string n) => Assert.False(NameFilter.Validate(n).Ok);

    [Theory]
    [InlineData("d.m")]
    [InlineData("<script>")]
    [InlineData("a_b")]
    public void Rejects_Symbols(string n) => Assert.False(NameFilter.Validate(n).Ok);
}
