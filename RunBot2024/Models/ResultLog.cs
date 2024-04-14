using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class ResultLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        private int _id;
        private string _message;
        private long _telegramId;
        private double _totalResult;
        private double _lastAddedResult;
        private DateTime _lastUpdated;

        public int Id { get => _id; set => _id = value; }
        public string Message { get => _message; set => _message = value; }
        public long TelegramId { get => _telegramId; set => _telegramId = value; }
        public double TotalResult { get => _totalResult; set => _totalResult = value; }
        public double LastAddedResult { get => _lastAddedResult; set => _lastAddedResult = value; }
        public DateTime LastUpdated { get => _lastUpdated; set => _lastUpdated = value; }
    }
}
