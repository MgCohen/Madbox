#pragma warning disable SCA0012
namespace Scaffold.MVVM.Binding
{
    public abstract class Converter<TSource, TTarget>
    {
        public virtual bool CanConvert(TSource source)
        {
            return source is not null;
        }

        public abstract TTarget Convert(TSource source);
    }
}
