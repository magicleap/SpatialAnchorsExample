namespace PersistentContentExample
{
    /// <summary>
    /// Basic interface that is used to create data that can be saved by the BindingsLocalStorage class.
    /// </summary>
    public interface IStorageBinding
    {
        public string Id { get; }
    }
}