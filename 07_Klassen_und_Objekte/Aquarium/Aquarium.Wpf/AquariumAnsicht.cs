using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Aquarium_Rechner;

public class AquariumAnsicht : FrameworkElement
{
    private Stopwatch uhr = new Stopwatch();
    private bool animationLaeuft = true;
    private bool geladen;
    private bool bildEreignisAngemeldet;
    private Window? fenster;
    private CultureInfo kultur = CultureInfo.GetCultureInfo("de-DE");

    public double LaengeCm { get; private set; } = 80;
    public double BreiteCm { get; private set; } = 35;
    public double HoeheCm { get; private set; } = 35;
    public bool GueltigeAbmessungen { get; private set; } = true;

    public bool AnimationLaeuft
    {
        get { return animationLaeuft; }
        set
        {
            animationLaeuft = value;
            AktualisiereUhr();
        }
    }

    private static Dictionary<string, Brush> farben = new Dictionary<string, Brush>();
    private static Brush glas = Farbe("#197CBDE5");
    private static Brush sand = Farbe("#DBCCAA");
    private static Brush pflanze = Farbe("#328F78");
    private static Brush hellePflanze = Farbe("#5CAE8B");
    private static Brush flosse = Farbe("#EFA13C");
    private static Brush dunkleFlosse = Farbe("#DD7A23");
    private static Brush fischKontur = Farbe("#AD521C");
    private static Brush augenfarbe = Farbe("#142C3E");
    private static Brush textfarbe = Farbe("#1768A6");
    private static Pen glaskante = new Pen(Farbe("#9A1768A6"), 1.4);
    private static Pen hintereKante = new Pen(Farbe("#552A80AC"), 1);
    private static Pen lichteKante = new Pen(Farbe("#BBFFFFFF"), 1.2);
    private static Brush wasser = Verlauf("#649DE2F5", "#9C59B8DA");
    private static Brush fischfarbe = Verlauf("#FFE3A2", "#EE7B26");
    private static Geometry koerper = Form("M -34,0 C -30,-22 17,-24 41,-9 C 52,-3 51,7 42,12 C 14,28 -24,21 -34,0 Z");
    private static Geometry schwanz = Form("M 0,0 C -12,-3 -24,-17 -36,-23 C -31,-5 -31,9 -36,22 C -21,15 -11,5 0,0 Z");
    private static Geometry rueckenflosse = Form("M -17,-16 C -6,-35 10,-34 23,-18 Z");
    private static Geometry bauchflosse = Form("M -14,14 C -9,30 5,31 15,16 Z");
    private static Geometry brustflosse = Form("M 4,5 C -3,12 -6,26 8,24 C 23,20 24,11 4,5 Z");
    private static Geometry kiemenForm = Form("M 22,-9 Q 17,1 22,12");
    private static Geometry mundForm = Form("M 44,5 Q 47,8 49,5");

    public AquariumAnsicht()
    {
        Loaded += Ansicht_Geladen;
        Unloaded += Ansicht_Entladen;
        IsVisibleChanged += Sichtbarkeit_Geaendert;
    }

    public void SetAbmessungen(double laenge, double breite, double hoehe)
    {
        if (!double.IsFinite(laenge) || !double.IsFinite(breite) || !double.IsFinite(hoehe)
            || laenge <= 0 || breite <= 0 || hoehe <= 0)
        {
            SetGueltig(false);
            return;
        }
        LaengeCm = laenge;
        BreiteCm = breite;
        HoeheCm = hoehe;
        SetGueltig(true);
    }

    public void SetGueltig(bool gueltig)
    {
        GueltigeAbmessungen = gueltig;
        AktualisiereUhr();
        InvalidateVisual();
    }

    private void Ansicht_Geladen(object sender, RoutedEventArgs e)
    {
        if (geladen) return;
        geladen = true;
        fenster = Window.GetWindow(this);
        if (fenster != null) fenster.StateChanged += Fensterzustand_Geaendert;
        AktualisiereUhr();
    }

    private void Ansicht_Entladen(object sender, RoutedEventArgs e)
    {
        geladen = false;
        AktualisiereUhr();
        if (fenster != null) fenster.StateChanged -= Fensterzustand_Geaendert;
        fenster = null;
        uhr.Stop();
    }

    private void Sichtbarkeit_Geaendert(object sender, DependencyPropertyChangedEventArgs e)
    {
        AktualisiereUhr();
    }

    private void Fensterzustand_Geaendert(object? sender, EventArgs e)
    {
        AktualisiereUhr();
    }

    private void AktualisiereUhr()
    {
        if (geladen && IsVisible && AnimationLaeuft && GueltigeAbmessungen
            && (fenster == null || fenster.WindowState != WindowState.Minimized))
        {
            uhr.Start();
            if (!bildEreignisAngemeldet)
            {
                CompositionTarget.Rendering += NeuesBild;
                bildEreignisAngemeldet = true;
            }
        }
        else
        {
            uhr.Stop();
            if (bildEreignisAngemeldet)
            {
                CompositionTarget.Rendering -= NeuesBild;
                bildEreignisAngemeldet = false;
            }
        }
    }

    private void NeuesBild(object? sender, EventArgs e)
    {
        // WPF bestimmt die Bildrate; die Bewegung hängt von der vergangenen Zeit ab.
        if (uhr.IsRunning) InvalidateVisual();
    }

    protected override void OnRender(DrawingContext zeichnung)
    {
        base.OnRender(zeichnung);
        if (ActualWidth < 1 || ActualHeight < 1) return;
        zeichnung.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
        if (!GueltigeAbmessungen) zeichnung.PushOpacity(0.25);
        ZeichneSzene(zeichnung, uhr.Elapsed.TotalSeconds);
        if (!GueltigeAbmessungen)
        {
            zeichnung.Pop();
            ZeichneText(zeichnung, "Bitte gültige Maße eingeben", new Point(ActualWidth / 2, ActualHeight / 2), 14);
        }
        zeichnung.Pop();
    }

    private void ZeichneSzene(DrawingContext zeichnung, double zeit)
    {
        AquariumGeometrie g = new AquariumGeometrie(LaengeCm, BreiteCm, HoeheCm, RenderSize);
        Point vorneLinks = g.Punkt(0, 0, 0);
        Point vorneRechts = g.Punkt(1, 0, 0);
        Point hintenLinks = g.Punkt(0, 0, 1);
        Point hintenRechts = g.Punkt(1, 0, 1);
        Point obenLinks = g.Punkt(0, 1, 0);
        Point obenRechts = g.Punkt(1, 1, 0);
        Point obenHintenLinks = g.Punkt(0, 1, 1);
        Point obenHintenRechts = g.Punkt(1, 1, 1);

        zeichnung.DrawEllipse(Farbe("#18377A96"), null,
            new Point((vorneLinks.X + hintenRechts.X) / 2, vorneLinks.Y + 9),
            (g.Laenge + g.TiefeX) * 0.49, Math.Min(9, 3 + g.TiefeY * 0.1));
        zeichnung.DrawGeometry(glas, hintereKante, Vieleck(obenHintenLinks, obenHintenRechts, hintenRechts, hintenLinks));
        zeichnung.DrawGeometry(glas, hintereKante, Vieleck(obenLinks, obenHintenLinks, hintenLinks, vorneLinks));
        zeichnung.DrawGeometry(wasser, null, Vieleck(g.Punkt(0, 0.82, 1), g.Punkt(1, 0.82, 1), hintenRechts, hintenLinks));
        zeichnung.DrawGeometry(sand, hintereKante, Vieleck(vorneLinks, vorneRechts, hintenRechts, hintenLinks));

        Geometry wasserUmriss = Vieleck(g.Punkt(0, 0.82, 0), vorneLinks, vorneRechts,
            hintenRechts, g.Punkt(1, 0.82, 1), g.Punkt(0, 0.82, 1));
        zeichnung.PushClip(wasserUmriss);
        zeichnung.DrawGeometry(wasser, null, wasserUmriss);
        zeichnung.DrawGeometry(Farbe("#BBDBCCAA"), null, Vieleck(vorneLinks, vorneRechts, hintenRechts, hintenLinks));
        ZeichneBoden(zeichnung, g, zeit);
        ZeichnePflanzen(zeichnung, g, zeit);
        ZeichneBlasen(zeichnung, g, zeit);
        ZeichneFisch(zeichnung, g, zeit);
        zeichnung.Pop();

        // Vorderes Glas und Wasseroberfläche liegen vor dem Fisch.
        zeichnung.DrawGeometry(Farbe("#16FFFFFF"), null, Vieleck(obenLinks, obenRechts, vorneRechts, vorneLinks));
        zeichnung.DrawGeometry(Farbe("#3AD0F4FA"), hintereKante,
            Vieleck(g.Punkt(0, 0.82, 0), g.Punkt(1, 0.82, 0), g.Punkt(1, 0.82, 1), g.Punkt(0, 0.82, 1)));
        ZeichneWellen(zeichnung, g, zeit);
        zeichnung.DrawGeometry(null, glaskante, Vieleck(obenLinks, obenRechts, vorneRechts, vorneLinks));
        zeichnung.DrawGeometry(glas, glaskante, Vieleck(obenRechts, obenHintenRechts, hintenRechts, vorneRechts));
        zeichnung.DrawGeometry(null, glaskante, Vieleck(obenLinks, obenRechts, obenHintenRechts, obenHintenLinks));
        zeichnung.DrawLine(lichteKante, g.Punkt(0.035, 0.10, 0), g.Punkt(0.035, 0.94, 0));
        zeichnung.DrawLine(lichteKante, g.Punkt(0.075, 0.48, 0), g.Punkt(0.075, 0.91, 0));

        if (GueltigeAbmessungen)
        {
            ZeichneText(zeichnung, "L " + LaengeCm.ToString("G4", kultur) + " cm",
                new Point((vorneLinks.X + vorneRechts.X) / 2, vorneLinks.Y + 16), 11);
            ZeichneText(zeichnung, "H " + HoeheCm.ToString("G4", kultur) + " cm",
                new Point(vorneLinks.X - 30, (obenLinks.Y + vorneLinks.Y) / 2), 11);
            ZeichneText(zeichnung, "B " + BreiteCm.ToString("G4", kultur) + " cm",
                new Point((vorneRechts.X + hintenRechts.X) / 2 + 8,
                    (vorneRechts.Y + hintenRechts.Y) / 2 + 16), 11);
        }
    }

    private void ZeichneBoden(DrawingContext zeichnung, AquariumGeometrie g, double zeit)
    {
        // Fest verteilte Kiesel: kein neues Zufallsmuster pro Bild.
        for (int i = 0; i < 32; i++)
        {
            double x = 0.03 + (i * 0.618 % 1) * 0.94;
            double z = 0.05 + (i * 0.414 % 1) * 0.90;
            double radius = Math.Min(g.Laenge, g.Hoehe) * (0.009 + i % 3 * 0.002);
            zeichnung.DrawEllipse(i % 2 == 0 ? Farbe("#B7AC8C") : Farbe("#EFE2C2"), null,
                g.Punkt(x, 0.025, z), radius, radius * 0.55);
        }
        for (int i = 0; i < 6; i++)
        {
            double x = 0.13 + i * 0.14;
            double bewegung = Math.Sin(zeit * 1.2 + i) * 0.02;
            zeichnung.DrawLine(new Pen(Farbe("#55FFFFFF"), 1),
                g.Punkt(x + bewegung, 0.02, 0.15), g.Punkt(x - bewegung, 0.02, 0.85));
        }
    }

    private void ZeichnePflanzen(DrawingContext zeichnung, AquariumGeometrie g, double zeit)
    {
        for (int gruppe = 0; gruppe < 3; gruppe++)
        {
            double x = gruppe == 0 ? 0.12 : gruppe == 1 ? 0.85 : 0.22;
            double z = gruppe == 2 ? 0.18 : 0.78;
            Point wurzel = g.Punkt(x, 0.035, z);
            for (int blatt = 0; blatt < 4; blatt++)
            {
                double hoehe = g.Hoehe * (0.25 + (blatt % 3) * 0.08);
                double neigung = (blatt - 1.5) * Math.Min(g.Laenge * 0.03, g.Hoehe * 0.05);
                double schwingen = Math.Sin(zeit * 1.6 + gruppe + blatt * 0.6) * g.Hoehe * 0.025;
                double dicke = Math.Min(g.Laenge, g.Hoehe) * 0.025;
                Point spitze = new Point(wurzel.X + neigung + schwingen, wurzel.Y - hoehe);
                StreamGeometry blattform = new StreamGeometry();
                using (StreamGeometryContext form = blattform.Open())
                {
                    form.BeginFigure(wurzel, true, true);
                    form.BezierTo(new Point(wurzel.X - dicke, wurzel.Y - hoehe * 0.3),
                        new Point(spitze.X - dicke, spitze.Y + hoehe * 0.2), spitze, true, false);
                    form.BezierTo(new Point(spitze.X + dicke, spitze.Y + hoehe * 0.3),
                        new Point(wurzel.X + dicke, wurzel.Y - hoehe * 0.2), wurzel, true, false);
                }
                zeichnung.DrawGeometry(blatt % 2 == 0 ? pflanze : hellePflanze, null, blattform);
            }
        }
    }

    private void ZeichneBlasen(DrawingContext zeichnung, AquariumGeometrie g, double zeit)
    {
        for (int i = 0; i < 12; i++)
        {
            double fortschritt = (zeit * 0.13 + i * 0.173) % 1;
            double x = (i % 2 == 0 ? 0.16 : 0.83) + Math.Sin(zeit * 1.7 + i) * 0.018;
            double y = 0.08 + fortschritt * 0.73;
            Point position = g.Punkt(x, y, 0.58);
            double radius = Math.Min(g.Hoehe, g.Laenge) * (0.008 + i % 3 * 0.003);
            zeichnung.PushOpacity(Math.Sin(fortschritt * Math.PI) * 0.8);
            zeichnung.DrawEllipse(Farbe("#28FFFFFF"), lichteKante, position, radius, radius);
            zeichnung.Pop();
        }
    }

    private void ZeichneWellen(DrawingContext zeichnung, AquariumGeometrie g, double zeit)
    {
        for (int i = 0; i < 3; i++)
        {
            double x = 0.20 + i * 0.25;
            double z = 0.45 + Math.Sin(zeit * 1.1 + i) * 0.12;
            zeichnung.DrawLine(lichteKante, g.Punkt(x, 0.82, z), g.Punkt(x + 0.12, 0.82, z));
        }
    }

    private void ZeichneFisch(DrawingContext zeichnung, AquariumGeometrie g, double zeit)
    {
        // Mehrere langsame Schwingungen ergeben eine ruhige, räumliche Schwimmbahn.
        double x = 0.5 + 0.29 * Math.Sin(zeit * 0.46) + 0.035 * Math.Sin(zeit * 1.1);
        double y = 0.46 + 0.13 * Math.Sin(zeit * 0.72 + 0.5);
        double z = 0.5 + 0.30 * Math.Sin(zeit * 0.29 + 0.8);
        double geschwindigkeitX = g.Laenge * (0.29 * 0.46 * Math.Cos(zeit * 0.46)
            + 0.035 * 1.1 * Math.Cos(zeit * 1.1)) + g.TiefeX * 0.30 * 0.29 * Math.Cos(zeit * 0.29 + 0.8);
        double geschwindigkeitY = -g.Hoehe * 0.13 * 0.72 * Math.Cos(zeit * 0.72 + 0.5)
            - g.TiefeY * 0.30 * 0.29 * Math.Cos(zeit * 0.29 + 0.8);
        // Beim Wenden wird der Fisch erst schmal, dann erscheint die andere Seite.
        double wendung = Math.Tanh(geschwindigkeitX / Math.Max(0.001, g.Laenge * 0.04 + g.TiefeX * 0.01));
        double groesse = Math.Min(g.Hoehe * 0.50, g.Laenge * 0.25) / 100;
        groesse = groesse * (1 - z * 0.17);
        double neigung = Math.Clamp(geschwindigkeitY / Math.Max(1, Math.Abs(geschwindigkeitX)), -0.25, 0.25) * 35;
        double flossenTakt = zeit * 9;
        Point position = g.Punkt(x, y, z);

        zeichnung.PushOpacity(1 - z * 0.16);
        zeichnung.PushTransform(new TranslateTransform(position.X, position.Y));
        zeichnung.PushTransform(new RotateTransform(neigung));
        zeichnung.PushTransform(new ScaleTransform(groesse * wendung, groesse));

        zeichnung.PushTransform(new TranslateTransform(-31, 0));
        zeichnung.PushTransform(new RotateTransform(Math.Sin(flossenTakt) * 16));
        zeichnung.PushTransform(new ScaleTransform(0.85 + Math.Cos(flossenTakt) * 0.15, 1));
        zeichnung.DrawGeometry(flosse, new Pen(fischKontur, 0.8), schwanz);
        for (int i = 0; i < 4; i++)
        {
            zeichnung.DrawLine(new Pen(dunkleFlosse, 0.7), new Point(-3, 0), new Point(-29, -14 + i * 9));
        }
        zeichnung.Pop();
        zeichnung.Pop();
        zeichnung.Pop();

        zeichnung.DrawGeometry(dunkleFlosse, null, rueckenflosse);
        zeichnung.PushTransform(new RotateTransform(Math.Sin(flossenTakt + 1) * 6, 0, 13));
        zeichnung.DrawGeometry(flosse, null, bauchflosse);
        zeichnung.Pop();
        zeichnung.DrawGeometry(fischfarbe, new Pen(fischKontur, 1), koerper);
        zeichnung.DrawEllipse(Farbe("#66FFF3CD"), null, new Point(4, -7), 22, 8);

        for (int i = 0; i < 3; i++)
        {
            double streifenX = -18 + i * 12;
            zeichnung.DrawLine(new Pen(Farbe("#38BA5B20"), 2),
                new Point(streifenX, -12), new Point(streifenX - 4, 13));
        }
        zeichnung.PushTransform(new RotateTransform(Math.Sin(flossenTakt + 0.8) * 22, 6, 7));
        zeichnung.DrawGeometry(flosse, new Pen(Farbe("#A8B7631F"), 0.6), brustflosse);
        zeichnung.Pop();

        double kieme = 1 + Math.Sin(zeit * 4.2) * 0.5;
        zeichnung.DrawGeometry(null, new Pen(fischKontur, kieme), kiemenForm);
        // Kurzes Blinzeln, sonst bleiben Auge und Lichtreflex sichtbar.
        if ((zeit + 1.4) % 7.8 < 0.14)
        {
            zeichnung.DrawLine(new Pen(augenfarbe, 1.6), new Point(30, -5), new Point(38, -5));
        }
        else
        {
            zeichnung.DrawEllipse(Farbe("#FFF8E3"), null, new Point(34, -5), 5.5, 5.5);
            zeichnung.DrawEllipse(augenfarbe, null, new Point(35, -5), 3.1, 3.8);
            zeichnung.DrawEllipse(Brushes.White, null, new Point(36, -6.5), 1.1, 1.1);
        }
        zeichnung.DrawGeometry(null, new Pen(fischKontur, 1), mundForm);
        zeichnung.Pop();
        zeichnung.Pop();
        zeichnung.Pop();
        zeichnung.Pop();
    }

    private void ZeichneText(DrawingContext zeichnung, string text, Point mitte, double groesse)
    {
        FormattedText beschriftung = new FormattedText(text, kultur, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), groesse, textfarbe, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        zeichnung.DrawText(beschriftung, new Point(mitte.X - beschriftung.Width / 2, mitte.Y - beschriftung.Height / 2));
    }

    private static Geometry Vieleck(params Point[] punkte)
    {
        StreamGeometry vieleck = new StreamGeometry();
        using (StreamGeometryContext form = vieleck.Open())
        {
            form.BeginFigure(punkte[0], true, true);
            for (int i = 1; i < punkte.Length; i++) form.LineTo(punkte[i], true, false);
        }
        return vieleck;
    }

    private static Brush Farbe(string farbcode)
    {
        if (farben.TryGetValue(farbcode, out Brush? vorhanden)) return vorhanden;
        SolidColorBrush farbe = new SolidColorBrush((Color)ColorConverter.ConvertFromString(farbcode));
        farbe.Freeze();
        farben.Add(farbcode, farbe);
        return farbe;
    }

    private static Brush Verlauf(string oben, string unten)
    {
        LinearGradientBrush farbe = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(oben),
            (Color)ColorConverter.ConvertFromString(unten), new Point(0, 0), new Point(0, 1));
        farbe.Freeze();
        return farbe;
    }

    private static Geometry Form(string beschreibung)
    {
        Geometry form = Geometry.Parse(beschreibung);
        form.Freeze();
        return form;
    }
}
