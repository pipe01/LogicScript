using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing
{
    public static class ErrorCodes
    {
        public const int VectorLengthGreaterZero = 1;
        public const int DuplicateConstant = 2;
        public const int ConstValueRequired = 3;
        public const int WhenConditionMissing = 4;
        public const int AssignmentRequired = 5;
        public const int FunctionReturnRequired = 6;
        public const int PortNameMissing = 7;
        public const int DuplicateDeclaration = 8;
        public const int FunctionAlreadyDefined = 9;
        public const int AssignmentTargetNotWritable = 10;
        public const int InfiniteLoop = 11;
        public const int LocalSizeOrValueRequired = 12;
        public const int DuplicateLocal = 13;
        public const int BreakOutsideLoop = 14;
        public const int ReturnOutsideFunction = 15;
        public const int ReturnValueMissing = 16;
        public const int ExpressionTooLarge = 17;
        public const int ConstantReferenceRequired = 18;
        public const int ExpressionReferenceNotReadable = 19;
        public const int IndexerOffsetMissing = 20;
        public const int SliceLengthZero = 21;
        public const int SliceOffsetOutOfBounds = 22;
        public const int SliceOutOfBounds = 23;
        public const int OperandRequired = 24;
        public const int SingleParameterRequired = 25;
        public const int OperatorNotImplemented = 26;
        public const int FunctionNotFound = 27;
        public const int FunctionArgumentCountMismatch = 28;
        public const int UnknownPort = 29;
        public const int VectoredPortRequiresIndex = 30;
        public const int LocalNotDeclared = 31;
        public const int UnknownInputPort = 32;
        public const int UnknownOutputPort = 33;
        public const int PortVectorTooShort = 34;
        public const int PortVectorTooLong = 35;
        public const int PortValueTooLarge = 36;
        public const int FirstStepCannotRepeat = 37;
        public const int OutputsMissing = 38;
        public const int PortValueMissing = 39;
        public const int DuplicatePort = 40;
    }

    internal class ErrorSink : IReadOnlyList<Error>
    {
        public Error this[int index] => Errors[index];

        public int Count => Errors.Count;

        internal bool HasANTLRError = false;

        private readonly IList<Error> Errors = [];

        public void AddError(string msg, SourceSpan span, bool isFatal = false, bool isANTLR = false, Severity severity = Severity.Error, object? data = null)
            => AddError(0, msg, span, isFatal, isANTLR, severity, data);

        public void AddError(int code, string msg, SourceSpan span, bool isFatal = false, bool isANTLR = false, Severity severity = Severity.Error, object? data = null)
        {
            if (isANTLR)
            {
                if (!HasANTLRError)
                    HasANTLRError = true;
                else
                    goto exit;
            }

            Errors.Add(new Error(code, msg, span, severity, isANTLR, data));

        exit:
            if (isFatal)
                throw new ParseCanceledException();
        }

        public void AddError(string msg, ICodeNode node, bool isFatal = false, bool isANTLR = false, Severity severity = Severity.Error, object? data = null)
            => AddError(0, msg, node, isFatal, isANTLR, severity, data);

        public void AddError(int code, string msg, ICodeNode node, bool isFatal = false, bool isANTLR = false, Severity severity = Severity.Error, object? data = null)
        {
            if (isANTLR)
            {
                if (!HasANTLRError)
                    HasANTLRError = true;
                else
                    goto exit;
            }

            if (!Errors.Any(o => o.Node == node))
            {
                Errors.Add(new Error(code, msg, node, severity, isANTLR, data));
            }

        exit:
            if (isFatal)
                throw new ParseCanceledException();
        }

        public IEnumerator<Error> GetEnumerator() => Errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Errors.GetEnumerator();

        public void AddVectorLengthGreaterZero(SourceSpan span) => AddError(ErrorCodes.VectorLengthGreaterZero, "Vector length must be greater than zero", span);
        public void AddDuplicateConstant(string name, int previousLine, SourceSpan span) => AddError(ErrorCodes.DuplicateConstant, $"The name '{name}' is already taken by previous constant at line {previousLine}", span);
        public void AddConstValueRequired(ICodeNode node) => AddError(ErrorCodes.ConstValueRequired, "Const declarations must have a constant value", node);
        public void AddWhenConditionMissing(SourceSpan span) => AddError(ErrorCodes.WhenConditionMissing, "Missing 'when' condition", span);
        public void AddAssignmentRequired(SourceSpan span) => AddError(ErrorCodes.AssignmentRequired, "Assignment block must contain an assignment", span);
        public void AddFunctionReturnRequired(SourceSpan span) => AddError(ErrorCodes.FunctionReturnRequired, "Function must return a value", span);
        public void AddPortNameMissing(SourceSpan span) => AddError(ErrorCodes.PortNameMissing, "Missing port name", span);
        public void AddDuplicateDeclaration(string name, int previousLine, SourceSpan span) => AddError(ErrorCodes.DuplicateDeclaration, $"The name '{name}' is already taken by previous declaration at line {previousLine}", span);
        public void AddFunctionAlreadyDefined(string name, SourceSpan span) => AddError(ErrorCodes.FunctionAlreadyDefined, $"Function \"{name}\" already defined", span);
        public void AddAssignmentTargetNotWritable(SourceSpan span) => AddError(ErrorCodes.AssignmentTargetNotWritable, "The left hand side of an assignment must be writable", span);
        public void AddInfiniteLoop(SourceSpan span) => AddError(ErrorCodes.InfiniteLoop, "Infinite loop detected", span, severity: Severity.Warning);
        public void AddLocalSizeOrValueRequired(SourceSpan span) => AddError(ErrorCodes.LocalSizeOrValueRequired, "You must specify a local's size or initialize it", span, isFatal: true);
        public void AddDuplicateLocal(string name, int previousLine, SourceSpan span) => AddError(ErrorCodes.DuplicateLocal, $"Identifier {name} already taken by declaration at line {previousLine}", span);
        public void AddBreakOutsideLoop(SourceSpan span) => AddError(ErrorCodes.BreakOutsideLoop, "Break statements can only be used inside loops", span);
        public void AddReturnOutsideFunction(SourceSpan span) => AddError(ErrorCodes.ReturnOutsideFunction, "Cannot return outside of a function", span);
        public void AddReturnValueMissing(SourceSpan span) => AddError(ErrorCodes.ReturnValueMissing, "Missing return value", span);
        public void AddExpressionTooLarge(int expressionSize, int maxSize, ICodeNode node, Severity severity) => AddError(ErrorCodes.ExpressionTooLarge, $"Cannot fit a {expressionSize} bits long number into {maxSize} bits", node, data: maxSize, severity: severity);
        public void AddConstantReferenceRequired(SourceSpan span) => AddError(ErrorCodes.ConstantReferenceRequired, "You can only reference constants from other constants", span, isFatal: true);
        public void AddExpressionReferenceNotReadable(SourceSpan span) => AddError(ErrorCodes.ExpressionReferenceNotReadable, "An identifier in an expression must be readable", span);
        public void AddIndexerOffsetMissing(SourceSpan span) => AddError(ErrorCodes.IndexerOffsetMissing, "Missing indexer offset", span);
        public void AddSliceLengthZero(SourceSpan span) => AddError(ErrorCodes.SliceLengthZero, "Slice length cannot be zero", span);
        public void AddSliceOffsetOutOfBounds(SourceSpan span) => AddError(ErrorCodes.SliceOffsetOutOfBounds, "Offset is out of bounds", span);
        public void AddSliceOutOfBounds(SourceSpan span) => AddError(ErrorCodes.SliceOutOfBounds, "Slice is out of bounds", span);
        public void AddOperandRequired(string name, SourceSpan span) => AddError(ErrorCodes.OperandRequired, $"{name} requires an operand", span);
        public void AddSingleParameterRequired(string name, SourceSpan span) => AddError(ErrorCodes.SingleParameterRequired, $"{name} takes a single parameter", span);
        public void AddOperatorNotImplemented(string operatorName, SourceSpan span) => AddError(ErrorCodes.OperatorNotImplemented, $"The {operatorName} operator is not yet implemented", span);
        public void AddFunctionNotFound(string name, SourceSpan span) => AddError(ErrorCodes.FunctionNotFound, $"Function \"{name}\" not found", span);
        public void AddFunctionArgumentCountMismatch(string name, int expected, int actual, SourceSpan span) => AddError(ErrorCodes.FunctionArgumentCountMismatch, $"Function \"{name}\" takes {expected} parameter(s) but {actual} were given", span);
        public void AddUnknownPort(string name, SourceSpan span) => AddError(ErrorCodes.UnknownPort, $"Unknown port '{name}'", span);
        public void AddVectoredPortRequiresIndex(SourceSpan span) => AddError(ErrorCodes.VectoredPortRequiresIndex, "Vectored port must be indexed", span);
        public void AddLocalNotDeclared(string name, SourceSpan span) => AddError(ErrorCodes.LocalNotDeclared, $"Local variable {name} is not declared", span);
        public void AddUnknownInputPort(string name, SourceSpan span) => AddError(ErrorCodes.UnknownInputPort, $"Unknown input port '{name}'", span);
        public void AddUnknownOutputPort(string name, SourceSpan span) => AddError(ErrorCodes.UnknownOutputPort, $"Unknown output port '{name}'", span);
        public void AddPortVectorTooShort(SourceSpan span) => AddError(ErrorCodes.PortVectorTooShort, "Not enough values to fill port vector", span);
        public void AddPortVectorTooLong(SourceSpan span) => AddError(ErrorCodes.PortVectorTooLong, "Too many values for port vector", span);
        public void AddPortValueTooLarge(SourceSpan span) => AddError(ErrorCodes.PortValueTooLarge, "Value doesn't fit in port", span);
        public void AddFirstStepCannotRepeat(SourceSpan span) => AddError(ErrorCodes.FirstStepCannotRepeat, "The first step on a case must not be a repetition", span);
        public void AddOutputsMissing(SourceSpan span) => AddError(ErrorCodes.OutputsMissing, "Missing outputs declaration", span);
        public void AddPortValueMissing(SourceSpan span) => AddError(ErrorCodes.PortValueMissing, "Missing port value", span);
        public void AddDuplicatePort(SourceSpan span) => AddError(ErrorCodes.DuplicatePort, "Duplicate port", span);
        public void AddBitLengthTooSmall(int bitLength, SourceSpan span) => AddError(ErrorCodes.ExpressionTooLarge, $"All bit lengths {bitLength} must be more than zero", span);
        public void AddBitLengthTooLarge(int bitLength, SourceSpan span) => AddError(ErrorCodes.ExpressionTooLarge, $"All bit lengths {bitLength} must be less than or equal to {BitsValue.BitSize}", span);
    }
}
