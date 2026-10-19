using PdfSharp.Fonts;
using System.IO;

namespace _3_WPF
{
    public class MyFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo("Arial");
        }

        public byte[] GetFont(string faceName)
        {
            // Путь к стандартному шрифту в Windows
            return File.ReadAllBytes(@"C:\Windows\Fonts\arial.ttf");
        }
    }

}
