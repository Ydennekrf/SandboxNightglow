

namespace ethra.V1
{
    /// <summary>
    /// ANy UI component that requires to be refreshed via the game loop needs to inherit this.
    /// </summary>
    public interface IUIRefresh
    {
        public bool needsRefresh { get; set; }
        void Refresh();
    }
}