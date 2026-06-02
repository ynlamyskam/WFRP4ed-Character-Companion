namespace WFRP_Character_Companion.Services.Content
{
    public interface IContentParser<TDto, TEntity>
    {
        List<TEntity> Parse(string json);
    }
}
