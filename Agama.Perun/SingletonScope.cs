namespace Agama.Perun
{
    public class SingletonScope : IPerunScope
    {
        private readonly PerunContainer _container;

        internal SingletonScope(PerunContainer container)
        {
            _container = container;
        }
        public object Context
        {
            get { return _container; }
        }
    }
}