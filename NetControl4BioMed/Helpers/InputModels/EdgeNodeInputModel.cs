﻿namespace NetControl4BioMed.Helpers.InputModels
{
    /// <summary>
    /// Represents the input model for an edge node.
    /// </summary>
    public class EdgeNodeInputModel
    {
        /// <summary>
        /// Represents the edge ID of the edge node.
        /// </summary>
        public string EdgeId { get; set; }

        /// <summary>
        /// Represents the node ID of the edge node.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Represents the type of the edge node.
        /// </summary>
        public string Type { get; set; }
    }
}