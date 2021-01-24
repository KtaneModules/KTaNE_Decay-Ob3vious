using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class decayScript : MonoBehaviour {

    //public stuff
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public TextMesh Text;
    public ParticleSystem Statuslight;
    public ParticleSystem Surround;
    public KMBombModule Module;

    //functionality
    private bool solved = false;
    private bool audioavailable = true;
    private int bitindex;
    private string quantumlog;
    private List<int?> input = new List<int?> { null };
    private List<int> answer = new List<int>();
    private List<bool> initbit;
    private List<bool> bits = new List<bool> { };
    private List<int> display = new List<int>();
    private List<int> sounds = new List<int>();

    //node system (necessary)
    private struct Node 
    {
        public Node(int prime)
        {
            Prime = prime;
            Active = false;
            Children = new List<Node> { };
        }
        public int Prime { get; }
        public bool Active { get; set; }
        public List<Node> Children { get; set; }
    }

    //fraction system (to make the code look prettier)
    private struct Frac
    {
        public Frac(int m, int n)
        {
            M = m;
            N = n;
        }
        public int M { get; set; }
        public int N { get; set; }
    }

    //logging
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    private KMSelectable.OnInteractHandler Press(int pos)
    {
        return delegate
        {
            if (!solved)
            {
                Buttons[pos].AddInteractionPunch(.5f);
                if (pos != 1 || input.First() != null)
                    Audio.PlaySoundAtTransform(Rnd.Range(1, 8).ToString(), Buttons[pos].transform);
                switch (pos)
                {
                    case 0:
                        if (input.Last() == null)
                            input[input.Count() - 1] = 0;
                        else
                            input[input.Count() - 1] *= 2;
                        break;
                    case 1:
                        if (input.First() == null)
                            StartCoroutine(Sounds(sounds));
                        else if (input.Last() == null)
                            CheckSolve();
                        else
                            input.Add(null);
                        break;
                    case 2:
                        if (input.Last() == null)
                            input[input.Count() - 1] = 1;
                        else
                            input[input.Count() - 1] = input.Last() * 2 + 1;
                        break;
                }
            }
            return false;
        };
    }

    void Awake()
    {
        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].OnInteract += Press(i);
            int j = i;
            Buttons[i].OnHighlight += delegate { if (!solved) { Text.text = Enumerable.Range(0, 3).Select(x => "<color='#ffffff" + (x == j ? "80" : "40") + "'>" + "ABCDEFGHIJKLMNO"[display[x]] + "</color>").Join(""); } };
            Buttons[i].OnHighlightEnded += delegate { if (!solved) { Text.text = display.Select(x => "ABCDEFGHIJKLMNO"[x]).Join(""); } };
        }
        _moduleID = _moduleIdCounter++;
    }

    void Start()
    {
        generation:
        int n = 0;
        while (PrimeFactors(n).Count() < 2 || EndnodeCount(PlantTree(0, n)) > 16 || EndnodeCount(PlantTree(0, n)) < 8)
            n = Rnd.Range(225, 3375);
        sounds = Enumerable.Range(1, 7).ToList().Shuffle();
        initbit = Enumerable.Range(0, 7).Select(x => sounds[(x + 1) % 7] - sounds[x % 7] > 0).ToList();
        Node tree = SetBool(PlantTree(0, n));
        tree.Active = true;
        Frac ratio = LogicCascade(tree);
        List<int> sequence = ReductiveCascade(ratio);
        List<int> indices = BaseConvert(n, sequence.Count());
        if (indices.Count() % 2 == 1)
            indices = new List<int> { 0 }.Concat(indices).ToList();
        List<Frac> fractions = Enumerable.Range(0, indices.Count() / 2).Select(x => new Frac(sequence[new int[] { indices[2 * x], indices[2 * x + 1] }.Max()], sequence[new int[] { indices[2 * x], indices[2 * x + 1] }.Min()])).ToList();
        for (int i = 0; i < fractions.Count(); i++)
            if (fractions[i].N != 0)
                fractions[i] = new Frac(fractions[i].M / GreatestCommonDivisor(fractions[i].M, fractions[i].N), fractions[i].N / GreatestCommonDivisor(fractions[i].M, fractions[i].N));
        Frac quantum = QuantumLogic(fractions);
        if (quantum.N == 0)
        {
            //Debug.LogFormat("[Decay #{0}] The quantum fraction got too unstable. Resetting...", _moduleID);
            bitindex = 0;
            bits = new List<bool> { };
            goto generation;
        }
        Debug.LogFormat("[Decay #{0}] The number is {1} or in base 15: {2}.", _moduleID, n, BaseConvert(n, 15).Join());
        Debug.LogFormat("[Decay #{0}] The obtained binary set is: {1}.", _moduleID, initbit.Select(x => x ? 1 : 0).Join(""));
        Debug.Log(Log(tree, 0, n, null));
        Debug.LogFormat("[Decay #{0}] The obtained ratio is: {1}/{2}.", _moduleID, ratio.M, ratio.N);
        Debug.LogFormat("[Decay #{0}] The reductive sequence is: {1}.", _moduleID, sequence.Join(", "));
        Debug.LogFormat("[Decay #{0}] The converted number is: {1}.", _moduleID, indices.Join());
        Debug.LogFormat("[Decay #{0}] The fractions (simplified) for QL are: {1}.", _moduleID, fractions.Select(x => x.M + "/" + x.N).Join(", "));
        Debug.LogFormat("[Decay #{0}] {1}.", _moduleID, quantumlog);
        Debug.LogFormat("[Decay #{0}] The quantum boolean is: {1}/{2}.", _moduleID, quantum.M, quantum.N);
        answer = FractionCascade(quantum);
        Debug.LogFormat("[Decay #{0}] The expected answer is: {1}.", _moduleID, answer.Join(", "));
        display = BaseConvert(n, 15);
        Text.text = display.Select(x => "ABCDEFGHIJKLMNO"[x]).Join("");
    }

    //utility functions
    private List<int> PrimeFactors(int number)
    {
        List<int> l = new List<int> { };
        for (int i = 2; i <= number; i++)
            while (number % i == 0)
            {
                l.Add(i);
                number /= i;
            }
        return l;
    }

    private int GreatestCommonDivisor(int a, int b)
    {
        while (a * b != 0)
        {
            int s = a % b;
            a = b;
            b = s;
        }
        return new int[] { a, b }.Max();
    }

    private int ReductiveModulo(int a, int b)
    {
        while (a >= b && b > 0)
        {
            a -= b;
            b--;
        }
        if (b == 0)
            return 0;
        else
            return a;
    }

    private List<int> BaseConvert(int n, int b)
    {
        List<int> l = new List<int> { };
        if (n == 0)
            l.Add(0);
        while (n > 0)
        {
            l.Add(n % b);
            n /= b;
        }
        l.Reverse();
        return l;
    }

    //module functions
    private Node PlantTree(int prime, int remainder)
    {
        Node node = new Node(prime);
        if (PrimeFactors(remainder).Count() == 1)
            return node;
        List<int> factors = PrimeFactors(remainder).Distinct().ToList();
        foreach (int factor in factors)
            node.Children.Add(PlantTree(factor, remainder / factor + 1));
        return node;
    }

    private int EndnodeCount(Node node)
    {
        int count = 0;
        if (node.Children.Count() == 0)
            count++;
        else
            foreach (Node child in node.Children)
                count += EndnodeCount(child);
        return count;
    }

    private string Log(Node node, int depth, int remainder, bool? logic)
    {
        string logging = string.Empty;
        if (depth == 0)
            logging += "[Decay #" + _moduleID + "] <=========[Node Tree (factor, remainder; branch, bit)]=========>";
        logging += "\n[Decay #" + _moduleID + "] " + Enumerable.Repeat("- ", depth).Join("") + "(" + node.Prime + ", " + remainder + "; " + (logic != null ? (node.Active ? "1" : "0") : "-") + ", " + (logic != null ? (logic.GetValueOrDefault() ? "1" : "0") : "1") + ")";
        if (logic == null)
            logic = true;
        foreach (Node child in node.Children)
            logging += Log(child, depth + 1, remainder / child.Prime + 1, logic.GetValueOrDefault() ^ child.Active);
        return logging;
    }

    private IEnumerator Sounds(List<int> names)
    {
        if (audioavailable)
        {
            audioavailable = false;
            for (int i = 0; i < names.Count(); i++)
            {
                Audio.PlaySoundAtTransform(names[i].ToString(), Module.transform);
                yield return new WaitForSeconds(.25f);
            }
            audioavailable = true;
        }
    }

    private bool RequestBit()
    {
        List<bool> neededbits = new List<bool> { };
        for (int i = 0; i <= bitindex; i++)
            neededbits.Add(initbit[i % initbit.Count()]);
        neededbits.Reverse();
        for (int i = 0; i < neededbits.Count(); i++)
            bits.Add(neededbits[i]);
        bool bit = bits[bitindex];
        bitindex++;
        return bit;
    }

    private Node SetBool(Node node)
    {
        Node newnode = new Node(node.Prime);
        if (node.Children.Count() == 0)
        {
            newnode = node;
            newnode.Active = RequestBit();
        }
        else
        {
            bool xor = false;
            foreach (Node child in node.Children)
            {
                newnode.Children.Add(SetBool(child));
                xor ^= newnode.Children.Last().Active;
            }
            newnode.Active = xor;
        }
        return newnode;
    }

    private Frac LogicCascade(Node node)
    {
        Frac frac = new Frac(0, 0);
        foreach (Node child in node.Children)
        {
            Node newchild = child;
            newchild.Active = child.Active ^ node.Active;
            Frac childfrac = LogicCascade(newchild);
            frac.M += childfrac.M + (newchild.Active ? newchild.Prime : 0);
            frac.N += childfrac.N + newchild.Prime;
        }
        return frac;
    }

    private List<int> ReductiveCascade(Frac frac)
    {
        List<int> sequence = new List<int> { frac.N, frac.M };
        while (!sequence.Contains(0))
            sequence.Add(ReductiveModulo(sequence[sequence.Count() - 2], sequence.Last()));
        return sequence;
    }

    private Frac QuantumLogic(List<Frac> quantumbools)
    {
        Frac fraction = new Frac(0, 1);
        quantumbools = quantumbools.Select(x => x.N == 0 ? new Frac(1, 2) : new Frac(x.M / GreatestCommonDivisor(x.M, x.N), x.N / GreatestCommonDivisor(x.M, x.N))).ToList();
        int p = 1;
        for (int i = 0; i < quantumbools.Count(); i++)
            p *= 2;
        List<bool> truthtable = new List<bool> { };
        for (int i = 0; i < p; i++)
            truthtable.Add(RequestBit());
        quantumlog = "The truth table is: " + truthtable.Select(x => x ? 1 : 0).Join("");
        for (int i = 0; i < p; i++)
            if (truthtable[i])
            {
                Frac product = new Frac(1, 1);
                int k = p;
                for (int j = 0; j < quantumbools.Count(); j++)
                {
                    k /= 2;
                    product = new Frac(product.M * (((i / k) % 2) == 1 ? quantumbools[j].M : (quantumbools[j].N - quantumbools[j].M)), product.N * quantumbools[j].N);
                    product = new Frac(product.M / GreatestCommonDivisor(product.M, product.N), product.N / GreatestCommonDivisor(product.M, product.N));
                }
                fraction = new Frac(fraction.M * product.N + fraction.N * product.M, fraction.N * product.N);
                if (GreatestCommonDivisor(fraction.M, fraction.N) == 0 || fraction.M < 0 || fraction.N < 0)
                    return new Frac(0, 0);
                fraction = new Frac(fraction.M / GreatestCommonDivisor(fraction.M, fraction.N), fraction.N / GreatestCommonDivisor(fraction.M, fraction.N));
            }
        return fraction;
    }

    private List<int> FractionCascade(Frac frac)
    {
        List<int> sequence = new List<int> { };
        while (frac.N != 0)
        {
            sequence.Add(frac.M / frac.N);
            frac = new Frac(frac.N, frac.M % frac.N);
        }
        return sequence;
    }

    private void CheckSolve()
    {
        bool good = false;
        if (input.Count() - 1 == answer.Count())
        {
            good = true;
            for (int i = 0; i < answer.Count(); i++)
                if (input[i].GetValueOrDefault() != answer[i])
                    good = false;
        }
        if (good)
        {
            Debug.LogFormat("[Decay #{0}] You submitted the correct answer. Module solved!", _moduleID);
            Module.HandlePass();
            StartCoroutine(Solve());
            StartCoroutine(SolveAudio());
        }
        else
        {
            Debug.LogFormat("[Decay #{0}] Your answer of {1} does not match the expected answer. Strike!", _moduleID, input.Where(x => x != null).Join(", "));
            Module.HandleStrike();
            StartCoroutine(Strike());
        }
        input = new List<int?> { null };
    }

    private IEnumerator Solve()
    {
        solved = true;
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            Statuslight.startColor = Color.Lerp(new Color(1, 1, 1), new Color(0, 1, 0), t);
            yield return null;
        }
        Statuslight.startColor = new Color(0, 1, 0);
        yield return null;
    }

    private IEnumerator SolveAudio()
    {
        Text.text = "LHI";
        string[] frames = { "LHI", "NHI", "MHI", "LLI", "LJI", "LII", "LHM", "LHK" };
        string song = Enumerable.Range(0, 28).Select(x => Rnd.Range(0, 28) <= x && x != 27 ? "0" : "1357642013576420135764201357"[x].ToString()).Join("");
        for (int j = 0; j < song.Length; j++)
        {
            yield return new WaitForSeconds(.25f);
            if (song[j] != '0')
                Audio.PlaySoundAtTransform(song[j].ToString(), Module.transform);
            Text.text = frames[song[j] - '0'];
        }
        yield return new WaitForSeconds(.25f);
        Text.text = "";
    }

    private IEnumerator Strike()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            Statuslight.startColor = Color.Lerp(new Color(1, 0, 0), new Color(1, 1, 1), t);
            yield return null;
        }
        Statuslight.startColor = new Color(1, 1, 1);
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} press l m r' to press those positions.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (Regex.IsMatch(command, @"^press\s((l|m|r)(\s?))+$"))
        {
            string[] set = { "l", "m", "r" };
            MatchCollection matches = Regex.Matches(command.Replace("press", ""), @"(l|m|r)");
            foreach (Match match in matches)
                foreach (Capture capture in match.Captures)
                {
                    Debug.Log(capture.ToString());
                    Buttons[Array.IndexOf(set, capture.ToString())].OnInteract();
                    yield return null;
                }

            yield return "strike";
            yield return "solve";
        }
        else
            yield return "sendtochaterror Invalid command.";
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        input = new List<int?> { null };
        yield return true;
        for (int i = 0; i < answer.Count(); i++)
        {
            List<int> press = BaseConvert(answer[i], 2).Select(x => x * 2).Concat(new int[] { 1 }).ToList();
            for (int j = 0; j < press.Count(); j++)
            {
                Buttons[press[j]].OnInteract();
                yield return true;
            }
        }
        Buttons[1].OnInteract();
        yield return true;
    }
}