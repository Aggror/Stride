using Xenko.Core;

namespace Xenko.Engine.Spline
{
    [DataContract]
    public struct SplineDebugInfo
    {
        private bool _points;
        private bool _segments;
        private bool _out;
        private bool _in;
        private bool _nodeLink;

        [DataMemberIgnore]
        public bool IsDirty { get; set; }

        public bool Points
        {
            get { return _points; }
            set
            {
                _points = value;
                IsDirty = true;
            }
        }

        public bool Segments
        {
            get { return _segments; }
            set
            {
                _segments = value;
                IsDirty = true;
            }
        }

        public bool NodesLink
        {
            get { return _nodeLink; }
            set
            {
                _nodeLink = value;
                IsDirty = true;
            }
        }

        public bool OutHandler
        {
            get { return _out; }
            set
            {
                _out = value;
                IsDirty = true;
            }
        }

        public bool InHandler
        {
            get { return _in; }
            set
            {
                _in = value;
                IsDirty = true;
            }
        }
    }
}
