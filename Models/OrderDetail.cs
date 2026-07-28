using DoAnDatVeXemPhim.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

public class OrderDetail
{
    public int Id { get; set; }

    [Column(TypeName = "decimal(18,2)")] // Nên thêm cái này để SQL không bị tròn số tiền
    public decimal PriceAtBooking { get; set; }

    public string TicketType { get; set; } = "Người lớn";

    public int OrderId { get; set; }
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    public int ShowtimeId { get; set; }
    [ForeignKey("ShowtimeId")]
    public virtual Showtime? Showtime { get; set; }

    public int SeatId { get; set; }
    [ForeignKey("SeatId")]
    public virtual Seat? Seat { get; set; }
}