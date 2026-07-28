using Microsoft.AspNetCore.Identity;

public class Wallet
{
    public int Id { get; set; }
    public string UserId { get; set; } // Khóa ngoại liên kết với IdentityUser
    public decimal Balance { get; set; } = 0; // Số dư ví
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual IdentityUser User { get; set; }
}