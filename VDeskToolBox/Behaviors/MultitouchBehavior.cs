using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace VDeskToolBox.Behaviors
{
    public class MultitouchBehavior : Behavior<UIElement>
    {
        private UIElement last;

        public ICommand Command
        {
            get { return ((ICommand)this.GetValue(MultitouchBehavior.CommandProperty)); }
            set { this.SetValue(MultitouchBehavior.CommandProperty, value); }
        }

        public static DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(MultitouchBehavior), new UIPropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.ManipulationStarting += AssociatedObject_ManipulationStarting;
            this.AssociatedObject.ManipulationInertiaStarting += AssociatedObject_ManipulationInertiaStarting;
            this.AssociatedObject.ManipulationDelta += AssociatedObject_ManipulationDelta;
        }

        private void AssociatedObject_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            //this just gets the source. 
            // I cast it to FE because I wanted to use ActualWidth for Center. You could try RenderSize as alternate
            var element = e.Source as FrameworkElement;
            if (element != null)
            {
                //e.DeltaManipulation has the changes 
                // Scale is a delta multiplier; 1.0 is last size,  (so 1.1 == scale 10%, 0.8 = shrink 20%) 
                // Rotate = Rotation, in degrees
                // Pan = Translation, == Translate offset, in Device Independent Pixels 

                var deltaManipulation = e.DeltaManipulation;
                var matrix = ((MatrixTransform)element.RenderTransform).Matrix;
                // find the old center; arguaby this could be cached 
                Point center = new Point(element.ActualWidth / 2, element.ActualHeight / 2);
                // transform it to take into account transforms from previous manipulations 
                center = matrix.Transform(center);
                //this will be a Zoom. 
                matrix.ScaleAt(deltaManipulation.Scale.X, deltaManipulation.Scale.Y, center.X, center.Y);
                // Rotation 
                matrix.RotateAt(e.DeltaManipulation.Rotation, center.X, center.Y);
                //Translation (pan) 
                matrix.Translate(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);

                ((MatrixTransform)element.RenderTransform).Matrix = matrix;

                e.Handled = true;


                if (e.IsInertial)
                {


                    Rect containingRect = new Rect(0, 0, System.Windows.SystemParameters.PrimaryScreenWidth, System.Windows.SystemParameters.PrimaryScreenHeight);

                    Rect shapeBounds = element.RenderTransform.TransformBounds(new Rect(element.RenderSize));
                    //Console.WriteLine(center.X.ToString() + " " + center.Y.ToString() + " " + containingRect.Contains(center).ToString());
                    // Check if the element is completely in the window.
                    // If it is not and intertia is occurring, stop the manipulation.
                    if (e.IsInertial && !containingRect.Contains(shapeBounds))
                    {

                        e.ReportBoundaryFeedback(e.DeltaManipulation);


                    }
                }

            }
        }

        private void AssociatedObject_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            // Decrease the velocity of the Rectangle's movement by 
            // 10 inches per second every second.
            // (10 inches * 96 DIPS per inch / 1000ms^2)
            e.TranslationBehavior = new InertiaTranslationBehavior()
            {
                InitialVelocity = e.InitialVelocities.LinearVelocity,
                DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0)
            };

            // Decrease the velocity of the Rectangle's resizing by 
            // 0.1 inches per second every second.
            // (0.1 inches * 96 DIPS per inch / (1000ms^2)
            e.ExpansionBehavior = new InertiaExpansionBehavior()
            {
                InitialVelocity = e.InitialVelocities.ExpansionVelocity,
                DesiredDeceleration = 0.1 * 96 / 1000.0 * 1000.0
            };

            // Decrease the velocity of the Rectangle's rotation rate by 
            // 2 rotations per second every second.
            // (2 * 360 degrees / (1000ms^2)
            e.RotationBehavior = new InertiaRotationBehavior()
            {
                InitialVelocity = e.InitialVelocities.AngularVelocity,
                DesiredDeceleration = 720 / (1000.0 * 1000.0)
            };
            e.Handled = true;
        }

        private void AssociatedObject_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
            e.ManipulationContainer = AssociatedObject;
            e.Handled = true;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.ManipulationDelta -= AssociatedObject_ManipulationDelta;
            this.AssociatedObject.ManipulationInertiaStarting -= AssociatedObject_ManipulationInertiaStarting;
            this.AssociatedObject.ManipulationStarting -= AssociatedObject_ManipulationStarting;
        }


        private void ManipulationDetected()
        {
            if (this.Command != null) this.Command.Execute(null);
        }
    }
}
