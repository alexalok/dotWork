namespace dotWork
{
    public interface IWork<TWorkOptions> where TWorkOptions : class, IWorkOptions
    {
        TWorkOptions Options { get; set; }

        void OnOptionsChanged();
    }
}
