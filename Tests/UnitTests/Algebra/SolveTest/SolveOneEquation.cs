﻿using AngouriMath;
using AngouriMath.Core;
using static AngouriMath.Entity.Number;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.Algebra
{
    public class SolveOneEquation
    {
        public static Entity.Variable x = nameof(x);

        /// <summary>Numerically checks if a root fits an equation</summary>
        internal static void AssertRoots(Entity equation, Entity.Variable toSub, Entity varValue, Integer? subValue = null)
        {
            subValue ??= 3;
            string eqNormal = equation.ToString();
            equation = equation.Substitute(toSub, varValue);
            // MUST be integer to correspond to integer coefficient of periodic roots
            var substitutions = new Dictionary<Entity.Variable, Integer>();
            foreach (var vr in equation.Vars)
                substitutions.Add(vr, subValue + substitutions.Count);
            equation = equation.Substitute(substitutions);
            var err = equation.Eval().Abs();
            Assert.True(err < 0.001m, $"\nError = {err}\n{eqNormal}\nWrong root: {toSub} = {varValue}");
        }

        static void AssertRootCount(Set roots, int target)
        {
            Assert.NotEqual(Set.PowerLevel.INFINITE, roots.Power);
            Assert.Equal(target, roots.Count);
        }

        void TestSolver(Entity expr, int rootCount, Integer? toSub = null, bool testNewton = false)
        {
            var roots = MathS.Settings.AllowNewton.As(false, () => expr.SolveEquation(x));
            AssertRootCount(roots, rootCount);
            foreach (var root in roots.FiniteSet())
                AssertRoots(expr, x, root, toSub);
            if (!testNewton) return;
            // TODO: Increase Newton precision
            roots = MathS.Settings.PrecisionErrorZeroRange.As(2e-16m, () => expr.SolveNt(x));
            AssertRootCount(roots, rootCount);
            foreach (var root in roots.FiniteSet())
                AssertRoots(expr, x, root, toSub);
        }

        [Fact]
        public void TestPolynomialToFix()
        {
            var eq = x.Pow(2) + 2 * x + 1;
            var roots = MathS.Settings.PrecisionErrorCommon.As(1e-8m, () =>
                MathS.Settings.NewtonSolver.As(new() { Precision = 100 }, () =>
                    eq.SolveNt(x)
                ));
            // AssertRootCount(roots, 1); TODO: remove // after fix
            foreach (var root in roots.FiniteSet())
                AssertRoots(eq, x, root);
        }

        [Theory]
        [InlineData("(x - 1) * (x - 2)", 2)]
        [InlineData("sqr(x) + 1", 2)]
        [InlineData("x2 + 2x + 2", 2)]
        [InlineData("2x2 + 4x + 2", 1)]
        [InlineData("x2 - 3x + 2", 2)]
        [InlineData("x3 + 3x2 + 3x + 1", 1)]
        [InlineData("x3 - 6x2 + 11x - 6", 3)]
        public void Polynomial(string expr, int rootCount) => TestSolver(expr, rootCount);

        [Theory]
        [InlineData("(x - goose) * (x - 3)", 2)]
        [InlineData("(x - momo) * (x - goose)", 2)]
        [InlineData("(x - goose) * (x + goose * momo) * (x - momo * 2)", 3)]
        // [InlineData("(x - goose) * (x - 2) * (sqr(x) - 4)", 3)] // TODO: Currently outputs 4 roots
        [InlineData("(x - goose) * (x - 3) * (sqr(x) - 4)", 4)]
        [InlineData("(x - goose) * (x - momo) * (x - quack) * (x - momo * goose * quack)", 4)]
        public void Vars(string expr, int rootCount)
        {
            var eq = MathS.FromString(expr);
            TestSolver(eq, rootCount);
            TestSolver(eq.Expand(), rootCount);
        }

        [Fact] public void MomoTest() => TestSolver("1/210 - (17*x)/210 + (101*x^2)/210 - (247*x^3)/210 + x^4", 4);
        [Fact] public void Logs() => TestSolver("log(x, 32) - 5", 1);
        [Fact] public void PiM1PowX() => TestSolver("pi - 1^x", 0, testNewton: true); // Check if it doesn't hang
        [Fact] public void ExpSimpl() => TestSolver("x^4 * x^y - 2", 1);

        [Theory]
        [InlineData("3x5 + 5x3", 3)]
        [InlineData("3x10 + 5x6", 5)]
        // Wolfram Alpha goes nuts, LOL: https://www.wolframalpha.com/input/?i=3x%5E5+%2B+5x%5E3+%3D-+a
        // [InlineData("3x5 + 5x3 + a")] // TODO: To doose (honk honk)
        public void Reduce(string expr, int rootCount) => TestSolver(expr, rootCount);
        [Fact(Skip = "Pending precision improvements in SolveNt")]
        public void ReduceNt()
        {
            Entity expr = "3x5 + 5x3";
            var roots = expr.SolveNt(x);
            AssertRootCount(roots, 3);
            foreach (var root in roots.FiniteSet())
                AssertRoots(expr, x, root);
        }

        [Theory]
        [InlineData("sin", 8)]
        [InlineData("cos", 8)]
        [InlineData("tan", 4)]
        [InlineData("cotan", 4)]
        [InlineData("arcsin", 4)]
        // TODO: arccos return analytically correct answer, but incorrect when substituting variables (as there are some Y such there is no X that arccos(X) = Y)
        // [InlineData("arccos", 4)]
        [InlineData("arctan", 4)]
        [InlineData("arccotan", 4)]
        public void InvertedFunctions(string func, int rootAmount)
        {
            Entity toRepl = func + "(x2 + 3)";
            Entity expr = MathS.Sqr(toRepl) + 0.3 * toRepl - 0.1 * MathS.Var("a");
            var roots = expr.SolveEquation(x);
            AssertRootCount(roots, rootAmount);
            foreach (var root in roots.FiniteSet())
                AssertRoots(expr.Substitute("a", 5), x, root.Substitute("n_1", 3).Substitute("a", 5));
        }

        private readonly List<Complex> KeyPoints = new List<Complex>
        {
            Complex.Create(0, 1),
            Complex.Create(1, 0),
            Complex.Create(-3, -3),
            Complex.Create(2, 2),
            Complex.Create(13, 13),
            Complex.Create(-9, +7),
            Complex.Create(0.5m, -0.5m),
            Complex.Create(-0.5m, 0.5m),
        };

        [Theory]
        [InlineData("x4 - x2 + 1")]
        [InlineData("x4 - x + 1")]
        [InlineData("x4 - x3 + 1")]
        [InlineData("x4 - x2 + x - x3 + 1")]
        [InlineData("(x2 - 1)2 - 2")]
        [InlineData("x4 - 2x2 - 1")]
        [InlineData("x4 - 2x2 - 2")]
        // TODO: Fix Newton Solver
        public void Ferrari(string input) => TestSolver(input, 4);

        [Theory]
        [InlineData("sin(x) + cos(x) - 1", 2)]
        [InlineData("sin(x) + cos(x) - 0.5", 2)]
        [InlineData("sin(x) + cos(x) - 2", 2)]
        [InlineData("sin(x)^2 + cos(x) - 1", 3)] // 2 pi n, -pi/2 + 2 pi n, pi/2 + 2 pi n
        [InlineData("3 * sin(2 * x + 1) - sin(x) - a", 4)]
        [InlineData("3 * sin(1 + 2 * x) - sin(x) - a", 4)]
        [InlineData("3 * sin(1 + x * 2) - sin(x) - a", 4)]
        [InlineData("3 * sin(x * 2 + 1) - sin(x) - a", 4)]
        [InlineData("3 * cos(2 * x + 1) - cos(x) - a", 4)]
        [InlineData("3 * cos(1 + 2 * x) - cos(x) - a", 4)]
        [InlineData("3 * cos(1 + x * 2) - cos(x) - a", 4)]
        [InlineData("3 * cos(x * 2 + 1) - cos(x) - a", 4)]
        [InlineData("sin(2x + 2) + sin(x + 1) - a", 4)] // Momo's Issue
        [InlineData("sin(2*x + 1) - sin(x) - 1", 4)]
        [InlineData("3 * sin(2 * x) - sin(x) - a", 4)]
        [InlineData("3 * sin(x * 2) - sin(x) - a", 4)]
        [InlineData("3 * sin(1 + x) - sin(x) - a", 2)]
        [InlineData("3 * sin(x + 1) - sin(x) - a", 2)]
        [InlineData("3 * cos(2 * x) - cos(x) - a", 4)]
        [InlineData("3 * cos(x * 2) - cos(x) - a", 4)]
        [InlineData("3 * cos(1 + x) - cos(x) - a", 2)]
        [InlineData("3 * cos(x + 1) - cos(x) - a", 2)]
        public void LinearTrigRoots(string expr, int rootCount) => TestSolver(expr, rootCount);

        [Theory]
        [InlineData("(x - b) / (x + a) + c", 1)]
        [InlineData("(x - b) / (x + a) + c / (x + a)", 1)]
        [InlineData("(x - b) / (x + a) + c / (x + a)2", 2)]
        [InlineData("(x - b) / (x + a) + c + (x - c) / (x + d)", 2, 11)]
        public void CDSolver(string expr, int rootCount, int? toSub = null) => TestSolver(expr, rootCount, toSub);

        [Theory]
        [InlineData("x + sqr(x + a) + c", 2)]
        [InlineData("x + sqr(x^0.1 + a) + c", 0)]
        [InlineData("(x + 6)^(1/6) + x + x3 + a", 0)]
        [InlineData("sqrt(x + 1) + sqrt(x + 2) + a + x", 0)]
        [InlineData("(x + 1)^(1/3) - x - a", 3)]
        public void FractionedPoly(string expr, int rootCount) => TestSolver(expr, rootCount);
    }
}