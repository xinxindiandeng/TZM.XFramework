﻿
using System;
using System.Reflection;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    public class NpgUpdateExpressionVisitor : ExpressionVisitorBase
    {
        private IDbQueryProvider _provider = null;
        private TableAliasCache _aliases = null;

        /// <summary>
        /// 初始化 <see cref="NpgUpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="expression">要访问的表达式</param>
        public NpgUpdateExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, Expression expression)
            : base(provider, aliases, expression)
        {
            _provider = provider;
            _aliases = aliases;
        }

        /// <summary>
        /// 访问成员初始化表达式，如 => new App() { Id = p.Id }
        /// </summary>
        /// <param name="node">要访问的成员初始化表达式</param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings == null || node.Bindings.Count == 0)
                throw new XFrameworkException("The Update<T> method requires at least one field to be updated.");

            for (int index = 0; index < node.Bindings.Count; index++)
            {
                MemberAssignment m = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember(m.Member.Name);
                _builder.Append(" = ");

                Type newType = node.Type;
                this.VisitWithoutRemark(x => this.VisitMemberAssignmentImpl(newType, m));

                if (index < node.Bindings.Count - 1)
                {
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }
            return node;
        }

        // => Name = "Name" 
        private void VisitMemberAssignmentImpl(Type newType, MemberAssignment m)
        {
            // 先添加当前字段的访问痕迹标记
            var member = m.Member;
            _visitedMark.Add(m.Member, newType);
            base.Visit(m.Expression);
        }

        /// <summary>
        /// 访问构造函数表达式，如 =>new  { Id = p.Id }
        /// </summary>
        /// <param name="node">构造函数调用的表达式</param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            // 匿名类的New
            if (node == null) return node;
            if (node.Arguments == null || node.Arguments.Count == 0 || node.Members.Count == 0)
                throw new XFrameworkException("The Update<T> method requires at least one field to be updated.");

            for (int index = 0; index < node.Arguments.Count; index++)
            {
                var m = node.Members[index];
                _builder.AppendMember(m.Name);
                _builder.Append(" = ");

                Type newType = node.Type;
                MemberInfo member = node.Members[index];
                Expression argument = node.Arguments[index];
                this.VisitWithoutRemark(x => this.VisitNewArgumentImpl(newType, m, argument));

                if (index < node.Arguments.Count - 1)
                {
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }

            return node;
        }

        // 访问 New 表达式中的参数
        private Expression VisitNewArgumentImpl(Type newType, MemberInfo member, Expression argument)
        {
            // 先添加当前字段的访问痕迹标记
            _visitedMark.Add(member, newType);
            return base.Visit(argument);
        }
    }
}
