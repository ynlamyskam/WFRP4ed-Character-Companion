namespace WFRP_Character_Companion.Services.Content
{
    public interface IContentImporter<T>
    {
        void Import(string filePath);
    }
}
