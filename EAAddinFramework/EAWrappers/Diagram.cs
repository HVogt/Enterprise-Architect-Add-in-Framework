﻿using System;
using System.Collections.Generic;
using System.Linq;

using UML=TSF.UmlToolingFramework.UML;

namespace TSF.UmlToolingFramework.Wrappers.EA {
  public class Diagram : UML.Diagrams.Diagram {
    internal global::EA.Diagram wrappedDiagram { get; set; }

    internal Model model;

    public Diagram(Model model, global::EA.Diagram wrappedDiagram ) {
      this.model = model;
      this.wrappedDiagram = wrappedDiagram;
    }
    
    /// all elements shown on this diagram.
    /// Currently only diagramObjectWrappers and relations
    public HashSet<UML.Diagrams.DiagramElement> diagramElements {
      get {
        List<UML.Diagrams.DiagramElement> returnedDiagramElements = 
          new List<UML.Diagrams.DiagramElement>
            ( this.diagramObjectWrappers );
        returnedDiagramElements.AddRange
          (this.diagramLinkWrappers.Cast<UML.Diagrams.DiagramElement>());
        return new HashSet<UML.Diagrams.DiagramElement>
          (returnedDiagramElements);
      }
      set { throw new NotImplementedException(); }
    }
    public List<UML.Classes.Kernel.Element> elements {
      get { throw new NotImplementedException(); }
      set { throw new NotImplementedException(); }
    }
    
    public HashSet<UML.Diagrams.DiagramElement> getDiagramElements<T>() 
      where T : UML.Classes.Kernel.Element 
    {
      HashSet<UML.Diagrams.DiagramElement> returnedDiagramElements =
        new HashSet<UML.Diagrams.DiagramElement>();
      foreach(UML.Diagrams.DiagramElement diagramElement 
              in this.diagramElements) 
      {
        if( diagramElement.element is T ) {
          returnedDiagramElements.Add(diagramElement);
        }
      }
      return returnedDiagramElements;
    }
    
    public String name {
      get { return this.wrappedDiagram.Name;  }
      set { this.wrappedDiagram.Name = value; }
    }

    /// find the diagramlink object (if any) that represents the given 
    /// relation
    internal global::EA.DiagramLink getDiagramLinkForRelation
      ( ConnectorWrapper relation )
    {
      foreach(global::EA.DiagramLink diagramLink 
              in this.wrappedDiagram.DiagramLinks)
      {
        if(diagramLink.ConnectorID == relation.wrappedConnector.ConnectorID) {
          return diagramLink;
        }
      }
      return null;
    }

    internal HashSet<DiagramLinkWrapper> diagramLinkWrappers {
      get {
        List<ConnectorWrapper> relations = this.getRelations();
        return new HashSet<DiagramLinkWrapper>
          (((Factory) this.model.factory).createDiagramElements
            (relations.Cast<UML.Classes.Kernel.Element>().ToList(),
             this).Cast<DiagramLinkWrapper>());
      }
    }
    internal HashSet<UML.Diagrams.DiagramElement> diagramObjectWrappers {
      get {
        return ((Factory) this.model.factory).createDiagramElements
          ( this.wrappedDiagram.DiagramObjects );
      }
    }


    
    /// <summary>
    /// the relations on a diagram in EA are sometimes expressed as 
    /// DiagramLink but not always.
    /// We are looking for all relations that have both their ends displayed 
    /// on the diagram.
    /// To make this a bit faster the list of id's is retrieved using an sql 
    /// query
    /// </summary>
    /// <returns>all reations ont he diagram</returns>
    internal virtual List<ConnectorWrapper> getRelations(){
      string SQLQuery = @"
      SELECT c.Connector_ID
        FROM (((( t_Connector c 
      INNER JOIN t_object source ON c.start_object_id = source.object_id )
      INNER JOIN t_object target ON c.end_object_id = target.object_id )
      INNER JOIN t_diagramObjects s ON source.object_id = s.object_id )
      INNER JOIN t_diagramObjects t ON target.object_id = t.object_id )
      WHERE s.Diagram_ID = " + this.wrappedDiagram.DiagramID +
     "  AND t.Diagram_ID = " + this.wrappedDiagram.DiagramID + ";";
      return this.model.getRelationsByQuery(SQLQuery);
    }
    
    internal global::EA.DiagramObject getdiagramObjectForElement
      ( ElementWrapper element)
    {
      foreach( global::EA.DiagramObject diagramObject 
          in this.wrappedDiagram.DiagramObjects ) 
      {
        if( diagramObject.ElementID == element.wrappedElement.ElementID ) {
          return diagramObject;
        }
      }
      return null;
    }

    public int height {
      get { return this.wrappedDiagram.cy;  }
      set { this.wrappedDiagram.cy = value; }
    }
    
    public int width {
      get { return this.wrappedDiagram.cx;  }
      set { this.wrappedDiagram.cx = value; }
    }
    public UML.Classes.Kernel.Element owner {
      get {
        if( this.wrappedDiagram.ParentID != 0 ) {
          return this.model.getElementWrapperByID
            ( this.wrappedDiagram.ParentID );
        } else {
          return this.model.getElementWrapperByPackageID
            ( this.wrappedDiagram.PackageID );
        }
      }
      set { throw new NotImplementedException(); }
    }
    
    public void save(){
      this.wrappedDiagram.Update();
    }
    
    internal int DiagramID {
      get { return this.wrappedDiagram.DiagramID; }
    }
    
    public void open() {
      this.model.currentDiagram = this;
    }
    
    public String comment {
      get { return this.wrappedDiagram.Notes;  }
      set { this.wrappedDiagram.Notes = value; }
    }
    public override int GetHashCode()
    {
        return this.DiagramID;
    }
    public override bool Equals(object obj)
    {
        return obj is Diagram && ((Diagram)obj).GetHashCode() == this.GetHashCode();
    }
  	
	public void select()
	{
		this.model.selectDiagram(this);
	}
    /// <summary>
    /// searches downward for the item with the given relative path
    /// This relative path includes the own name
    /// </summary>
    /// <param name="relativePath">list of names inlcuding the own name</param>
    /// <returns>the item matching the path</returns>
	public TSF.UmlToolingFramework.UML.UMLItem getItemFromRelativePath(List<string> relativePath)
	{
		UML.UMLItem item = null;
		List<string> filteredPath = new List<string>(relativePath);
		if (ElementWrapper.filterName( filteredPath,this.name))
		{
	    	if (filteredPath.Count ==1)
	    	{
	    		item = this;
	    	}
		}
		return item; 
	}
	public string fqn 
	{
		get 
		{
			string nodepath = string.Empty;
			if (this.owner != null)
			{
				nodepath = this.owner.fqn;
			}
			if (this.name.Length > 0)
			{
				if (nodepath.Length > 0) 
				{
					nodepath = nodepath + ".";
				}
				nodepath = nodepath + this.name;
			}			
			return nodepath;
		}
	}
	/// <summary>
	/// returns all operations called in this sequence diagram
	/// </summary>
	/// <returns>all operations called in this sequence diagram</returns>
	public List<UML.Classes.Kernel.Operation> getCalledOperations()
	{
		List<UML.Classes.Kernel.Operation> calledOperations = new List<UML.Classes.Kernel.Operation>();
		foreach ( DiagramLinkWrapper linkwrapper in this.diagramLinkWrappers) 
		{
			Message message = linkwrapper.relation as Message;
			if (message != null)
			{
				UML.Classes.Kernel.Operation operation = message.calledOperation;
				if (operation != null)
				{
					calledOperations.Add(operation);
				}
			}
		}
		return calledOperations;
	}
	/// <summary>
	/// opens the (standard) properties dialog in EA
	/// </summary>
    public void openProperties()
	{
		this.model.openProperties(this);
	}
  	
	public void selectItem(UML.UMLItem itemToSelect)
	{
		if (itemToSelect is Operation)
		{
			bool found = false;
			//if the item is a relation or an operation then search through the links first
			foreach (DiagramLinkWrapper diagramLinkWrapper in this.diagramLinkWrappers)
			{
				if (itemToSelect is Operation
				   && diagramLinkWrapper.relation is Message)
				{
					Message message = (Message)diagramLinkWrapper.relation;
					if (itemToSelect.Equals(message.calledOperation))
					{
						this.wrappedDiagram.SelectedConnector = message.wrappedConnector;
						found = true;
						//done, no need to loop further
						break;
					}
				}
			}
			//The operation could also be called in an Action.
			if (!found)
			{
				List<UML.Actions.BasicActions.CallOperationAction> actions = ((Operation)itemToSelect).getDependentCallOperationActions().ToList();
				List<UML.Diagrams.DiagramElement> diagramObjects = this.diagramObjectWrappers.ToList();
				
				foreach (Action  action in actions)
				{
					//try to find an diagramObjectwrapper that refrences the action
					UML.Diagrams.DiagramElement diagramObject = diagramObjects.Find(
						x => x.element.Equals(action));
					if (diagramObject != null)
					{
						//found it, select the action and break out of for loop
						this.selectItem(action);
						found = true;
						break;
					}
				}
			}
			if (!found)
			{
				//can't find a message on this diagram that calls the operation.
				//then we try it with the operations parent
				this.selectItem(((Operation)itemToSelect).owner);
				
			}
		}
		else if (itemToSelect is ConnectorWrapper)
		{
			this.wrappedDiagram.SelectedConnector = ((ConnectorWrapper)itemToSelect).wrappedConnector;
			//check if it worked
			if (wrappedDiagram.SelectedConnector == null
			   && itemToSelect is Message)
			{
				this.selectItem(((Message)itemToSelect).calledOperation);
			}
		}
		else if (itemToSelect is ElementWrapper)
		{
			ElementWrapper elementToSelect = (ElementWrapper)itemToSelect;
			this.wrappedDiagram.SelectedObjects.AddNew(elementToSelect.wrappedElement.ElementID.ToString(),
			                                           elementToSelect.wrappedElement.Type);
		}
	}
  	
	public HashSet<TSF.UmlToolingFramework.UML.Profiles.Stereotype> stereotypes {
		get 
		{
			 return ((Factory)this.model.factory).createStereotypes(this, this.wrappedDiagram.Stereotype );
		}
	}
  }
}
