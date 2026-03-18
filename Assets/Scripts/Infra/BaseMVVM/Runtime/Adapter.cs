#pragma warning disable SCA0012
namespace Scaffold.MVVM.Binding
{
    public abstract class Adapter<TTarget>
    {
        public virtual bool CanAdapt(TTarget target)
        {
            return target is not null;
        }

        public abstract TTarget Resolve(TTarget target);
    }
}
