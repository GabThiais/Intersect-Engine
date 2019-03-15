﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Intersect.Server.Core.CommandParsing.Commands;
using JetBrains.Annotations;

namespace Intersect.Server.Core.CommandParsing.Arguments
{
    public sealed class ArgumentValuesMap : IEnumerable<KeyValuePair<ICommandArgument, ArgumentValues>>
    {
        [NotNull] private readonly IDictionary<ICommandArgument, ArgumentValues> mValuesMap;

        public ArgumentValuesMap([CanBeNull] IEnumerable<KeyValuePair<ICommandArgument, ArgumentValues>> pairs = null)
        {
            mValuesMap = pairs?.ToDictionary(pair => pair.Key, pair => pair.Value) ??
                         new Dictionary<ICommandArgument, ArgumentValues>();
        }

        [NotNull]
        public ImmutableDictionary<ICommandArgument, ArgumentValues> Values =>
            mValuesMap.ToImmutableDictionary() ?? throw new InvalidOperationException();

        public IEnumerator<KeyValuePair<ICommandArgument, ArgumentValues>> GetEnumerator() =>
            mValuesMap.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [CanBeNull]
        public ArgumentValues Find([NotNull] ICommandArgument argument)
        {
            return mValuesMap.TryGetValue(argument, out var values) ? values : null;
        }

        [CanBeNull]
        public TValue Find<TValue>([NotNull] ICommandArgument argument, int index = 0)
        {
            var argumentValues = Find(argument);
            return argumentValues == null
                ? argument.DefaultValueAsType<TValue>()
                : argumentValues.ToTypedValue<TValue>(index);
        }

        [CanBeNull]
        public IEnumerable<TValues> FindAll<TValues>([NotNull] ICommandArgument argument)
        {
            return Find(argument)?.ToTypedValues<TValues>() ?? argument.DefaultValueAsType<IEnumerable<TValues>>();
        }

        [CanBeNull]
        public TValue Find<TValue>([NotNull] CommandArgument<TValue> argument, int index = 0)
        {
            return Find<TValue>(argument as ICommandArgument, index);
        }

        [CanBeNull]
        public IEnumerable<TValues> FindAll<TValues>([NotNull] ArrayCommandArgument<TValues> argument)
        {
            return FindAll<TValues>(argument as ICommandArgument);
        }

        [NotNull]
        public ParserResult AsResult(ICommand command = null)
        {
            return new ParserResult(command, this);
        }

        [NotNull]
        public ParserResult<TCommand> AsResult<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            return new ParserResult<TCommand>(command, this);
        }
    }
}