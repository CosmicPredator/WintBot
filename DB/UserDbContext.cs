using Microsoft.EntityFrameworkCore;

namespace WintBot;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options): base(options) 
    {
        Database.EnsureCreated();
    }
    
    public DbSet<User> UserList {get; set;} = null!;  

    public DbSet<NumberGame> NumberGameList { get; set; } = null!;

    public DbSet<WordGameModel> WordGameList {get; set; } = null!;
}