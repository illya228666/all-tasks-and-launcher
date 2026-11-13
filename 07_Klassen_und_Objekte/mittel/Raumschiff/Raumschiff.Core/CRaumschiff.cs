using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raumschiff.Core
{
    public class CRaumschiff : INotifyPropertyChanged
    {
        private const int maxXPos = 800;
        private const int maxYPos = 600;

        private int m_XPos;
        private int m_YPos;
        private int m_Energie;
        private double m_Geschwindigkeit;

        public CRaumschiff() :this(0, 0, 1000, 0) { Log("Raumschiff mit parameterlosem Konstruktor wurde erstellt."); }
        public CRaumschiff(int xPos, int yPos) :this(0, 0, 1000, 0)
        {
            XPos = xPos;
            YPos = yPos;
            Log("Raumschiff mit überladenem Konstruktor wurde erstellt.");
        }
        private CRaumschiff(int xPos, int yPos, int energie, double geschwindigkeit)
        {
            m_XPos = xPos;
            m_YPos = yPos;
            Energie = energie;
            Geschwindigkeit = geschwindigkeit;
        }

        public int XPos
        {
            get { return m_XPos; }
            set
            {
                if (value < 0 || value > maxXPos)
                {
                    value = 0;
                    Log("X-Koordinate ungültig. Wurde auf 0 gesetzt.");
                }
                else Log("X-Koordinate gültig.");
                if (m_XPos == value) return;
                m_XPos = value;
                OnPropertyChanged();
            }
        }

        public int YPos
        {
            get { return m_YPos; }
            set
            {
                if (value < 0 || value > maxYPos)
                {
                    value = 0;
                    Log("Y-Koordinate ungültig. Wurde auf 0 gesetzt.");
                }
                else Log("Y-Koordinate gültig.");
                if (m_YPos == value) return;
                m_YPos = value;
                OnPropertyChanged();
            }
        }

        public int Energie
        {
            get { return m_Energie; }
            set
            {
                if (m_Energie == value) return;
                m_Energie = value;
                OnPropertyChanged();
            }
        }

        public double Geschwindigkeit
        {
            get { return m_Geschwindigkeit; }
            set
            {
                if (m_Geschwindigkeit.Equals(value)) return;
                m_Geschwindigkeit = value;
                OnPropertyChanged();
            }
        }

        public void MoveForward()
        {
            int vorher = m_YPos;
            if (m_YPos + m_Geschwindigkeit < maxYPos) m_YPos += (int)m_Geschwindigkeit;
            else Log("Uperlis w senku");
            if (m_YPos != vorher) OnPropertyChanged(nameof(YPos));
        }
        public void MoveBack()
        {
            int vorher = m_YPos;
            if (m_YPos - m_Geschwindigkeit > 0) m_YPos -= (int)m_Geschwindigkeit;
            else Log("Uperlis w senku");
            if (m_YPos != vorher) OnPropertyChanged(nameof(YPos));
        }
        public void MoveLeft()
        {
            int vorher = m_XPos;
            if (m_XPos + m_Geschwindigkeit < 0) m_XPos -= (int)m_Geschwindigkeit;
            else Log("Uperlis w senku");
            if (m_XPos != vorher) OnPropertyChanged(nameof(XPos));
        }
        public void MoveRight()
        {
            int vorher = m_YPos;
            if (m_YPos + m_Geschwindigkeit < maxYPos) m_YPos += (int)m_Geschwindigkeit;
            else Log("Uperlis w senku");
            if (m_YPos != vorher) OnPropertyChanged(nameof(YPos));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static event Action<string> OnMessage;
        private static void Log(string message)
        {
            OnMessage?.Invoke(message);
        }

        ~CRaumschiff() { Log("Raumschiff wurde zerstört."); }

    }
}
