namespace TrashRoyale.Match
{
    public enum Team { Player, Enemy }

    public static class TeamExt
    {
        public static Team Other(this Team t) => t == Team.Player ? Team.Enemy : Team.Player;
    }
}
