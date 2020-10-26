namespace DeathReimagined
{
    public class Skeleton : KMonoBehaviour
    {
        private Reactable reactable;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            reactable = Decomposition.CreateRottenReactable(gameObject);
            GetComponent<KSelectable>().AddStatusItem(Db.Get().DuplicantStatusItems.Rotten);
        }

        protected override void OnCleanUp()
        {
            reactable.Cleanup();
            base.OnCleanUp();
        }
    }
}
