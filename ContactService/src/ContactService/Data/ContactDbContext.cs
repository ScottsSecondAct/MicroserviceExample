using ContactService.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactService.Data;

public class ContactDbContext : DbContext
{
  public ContactDbContext(DbContextOptions<ContactDbContext> options) : base(options) { }

  public DbSet<Contact> Contacts => Set<Contact>();
}
