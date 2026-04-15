

namespace ethra.V1
{
    public interface IResolveable
    {
        public int ResolveOrder { get; }
        void Resolve();

        void Resolve(object obj);


    }
}