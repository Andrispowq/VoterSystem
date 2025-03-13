namespace VoterSystem.DataAccess.Model;

public interface ISoftDeletable
{
    public DateTime? DeletedAt { get; }
}