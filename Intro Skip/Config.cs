
using IPA.Config.Stores;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace IntroSkip
{
    internal class Config
    {
        public virtual bool AllowIntroSkip { get; set; } = true;
        public virtual bool AllowIntermissionSkip { get; set; } = true;
        public virtual bool AllowOutroSkip { get; set; } = true;
    }
}