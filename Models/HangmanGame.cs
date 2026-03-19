using System.Collections.Generic;
using System.Linq;

namespace WebAppSandbox.Models
{
    public class HangmanGame
    {
        public string Word { get; set; }
        public List<char> GuessedLetters { get; set; } = new List<char>();
        public int MaxAttempts { get; set; } = 6;

        public HangmanGame(string word)
        {
            Word = word.ToLower();
        }

        public string GetMaskedWord()
        {
            return string.Join(" ", Word.Select(c => 
                GuessedLetters.Contains(c) ? c : '_'));
        }

        public void Guess(char letter)
        {
            letter = char.ToLower(letter);
            if (!GuessedLetters.Contains(letter))
            {
                GuessedLetters.Add(letter);
            }
        }

        public int WrongAttempts()
        {
            return GuessedLetters.Count(c => !Word.Contains(c));
        }

        public bool IsWon()
        {
            return Word.All(c => GuessedLetters.Contains(c));
        }

        public bool IsLost()
        {
            return WrongAttempts() >= MaxAttempts;
        }
    }
}